using Ape.Data;
using Ape.Models;
using Ape.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Ape.Services;

public class ShoppingCartService(
    ApplicationDbContext context,
    ISystemSettingsService settingsService,
    ILogger<ShoppingCartService> logger) : IShoppingCartService
{
    private readonly ApplicationDbContext _context = context;
    private readonly ISystemSettingsService _settingsService = settingsService;
    private readonly ILogger<ShoppingCartService> _logger = logger;

    public async Task<CartViewModel> GetCartAsync(string userId)
    {
        var cart = await GetOrCreateCartAsync(userId);

        var items = await _context.ShoppingCartItems
            .AsNoTracking()
            .Include(ci => ci.Product)
                .ThenInclude(p => p!.Images.OrderBy(i => i.SortOrder))
            .Where(ci => ci.CartID == cart.CartID)
            .Select(ci => new CartItemViewModel
            {
                CartItemId = ci.CartItemID,
                ProductId = ci.ProductID,
                ProductName = ci.Product!.Name,
                ProductSlug = ci.Product.Slug,
                ProductType = ci.Product.ProductType,
                ThumbnailUrl = ci.Product.Images.Any()
                    ? "/store/products/" + (ci.Product.Images.FirstOrDefault(i => i.IsPrimary) ?? ci.Product.Images.First()).FileName
                    : null,
                Quantity = ci.Quantity,
                UnitPrice = ci.UnitPrice,
                LineTotal = ci.UnitPrice * ci.Quantity,
                IsInStock = ci.Product.ProductType != ProductType.Physical || !ci.Product.TrackInventory || ci.Product.StockQuantity >= ci.Quantity,
                AvailableStock = ci.Product.StockQuantity,
                TrackInventory = ci.Product.TrackInventory
            })
            .ToListAsync();

        var subtotal = items.Sum(i => i.LineTotal);
        var hasPhysical = items.Any(i => i.ProductType == ProductType.Physical);
        var hasDigital = items.Any(i => i.ProductType == ProductType.Digital);
        var hasOutOfStock = items.Any(i => !i.IsInStock);

        decimal shippingCost = 0;
        string? freeShippingMessage = null;

        if (hasPhysical)
        {
            var flatRate = await _settingsService.GetSettingAsync("Store__FlatRateShipping", "5.99");
            shippingCost = decimal.TryParse(flatRate, out var rate) ? rate : 5.99m;

            var freeThreshold = await _settingsService.GetSettingAsync("Store__FreeShippingThreshold", "");
            if (decimal.TryParse(freeThreshold, out var threshold) && threshold > 0)
            {
                if (subtotal >= threshold)
                {
                    shippingCost = 0;
                    freeShippingMessage = "Free shipping applied!";
                }
                else
                {
                    var remaining = threshold - subtotal;
                    freeShippingMessage = $"Add {remaining:C} more for free shipping!";
                }
            }
        }

        return new CartViewModel
        {
            CartId = cart.CartID,
            Items = items,
            Subtotal = subtotal,
            ShippingCost = shippingCost,
            Total = subtotal + shippingCost,
            HasPhysicalItems = hasPhysical,
            HasDigitalItems = hasDigital,
            HasOutOfStockItems = hasOutOfStock,
            ItemCount = items.Sum(i => i.Quantity),
            FreeShippingMessage = freeShippingMessage
        };
    }

    public async Task<StoreOperationResult> AddItemAsync(string userId, int productId, int quantity, bool isMember)
    {
        if (quantity < 1)
            return StoreOperationResult.Failed("Quantity must be at least 1.");

        var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductID == productId && p.IsActive);
        if (product == null)
            return StoreOperationResult.Failed("Product not found.");

        if (product.ProductType == ProductType.Subscription)
            return StoreOperationResult.Failed("Subscription products cannot be added to cart. Use the Subscribe button instead.");

        if (product.ProductType == ProductType.Physical && product.TrackInventory && product.StockQuantity < quantity)
            return StoreOperationResult.Failed($"Sorry, only {product.StockQuantity} available in stock.");

        // Determine price (member price if applicable)
        var unitPrice = product.Price;
        if (isMember && product.MemberPrice.HasValue && product.MemberPrice < product.Price)
            unitPrice = product.MemberPrice.Value;

        var cart = await GetOrCreateCartAsync(userId);

        // Check if product already in cart
        var existingItem = await _context.ShoppingCartItems
            .FirstOrDefaultAsync(ci => ci.CartID == cart.CartID && ci.ProductID == productId);

        if (existingItem != null)
        {
            var newQty = existingItem.Quantity + quantity;
            if (product.ProductType == ProductType.Physical && product.TrackInventory && product.StockQuantity < newQty)
                return StoreOperationResult.Failed($"Cannot add more. Only {product.StockQuantity} available.");

            // Digital products: max quantity of 1
            if (product.ProductType == ProductType.Digital)
                return StoreOperationResult.Failed("This digital product is already in your cart.");

            existingItem.Quantity = newQty;
            existingItem.UnitPrice = unitPrice;
        }
        else
        {
            // Digital products always quantity 1
            if (product.ProductType == ProductType.Digital)
                quantity = 1;

            _context.ShoppingCartItems.Add(new ShoppingCartItem
            {
                CartID = cart.CartID,
                ProductID = productId,
                Quantity = quantity,
                UnitPrice = unitPrice,
                AddedDate = DateTime.UtcNow
            });
        }

        cart.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var cartCount = await GetCartItemCountAsync(userId);
        _logger.LogInformation("User {UserId} added product {ProductId} (qty: {Quantity}) to cart", userId, productId, quantity);

        return new StoreOperationResult { Success = true, EntityId = cartCount, Message = $"{product.Name} added to cart." };
    }

    public async Task<StoreOperationResult> UpdateQuantityAsync(string userId, int cartItemId, int quantity)
    {
        if (quantity < 1)
            return StoreOperationResult.Failed("Quantity must be at least 1.");

        var cart = await _context.ShoppingCarts.FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart == null)
            return StoreOperationResult.Failed("Cart not found.");

        var item = await _context.ShoppingCartItems
            .Include(ci => ci.Product)
            .FirstOrDefaultAsync(ci => ci.CartItemID == cartItemId && ci.CartID == cart.CartID);

        if (item == null)
            return StoreOperationResult.Failed("Item not found in cart.");

        if (item.Product?.ProductType == ProductType.Physical && item.Product.TrackInventory && item.Product.StockQuantity < quantity)
            return StoreOperationResult.Failed($"Only {item.Product.StockQuantity} available in stock.");

        item.Quantity = quantity;
        cart.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return StoreOperationResult.SucceededNoId("Cart updated.");
    }

    public async Task<StoreOperationResult> RemoveItemAsync(string userId, int cartItemId)
    {
        var cart = await _context.ShoppingCarts.FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart == null)
            return StoreOperationResult.Failed("Cart not found.");

        var item = await _context.ShoppingCartItems.FirstOrDefaultAsync(ci => ci.CartItemID == cartItemId && ci.CartID == cart.CartID);
        if (item == null)
            return StoreOperationResult.Failed("Item not found in cart.");

        _context.ShoppingCartItems.Remove(item);
        cart.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return StoreOperationResult.SucceededNoId("Item removed from cart.");
    }

    public async Task ClearCartAsync(string userId)
    {
        var cart = await _context.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart != null)
        {
            _context.ShoppingCartItems.RemoveRange(cart.Items);
            cart.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetCartItemCountAsync(string userId)
    {
        var cart = await _context.ShoppingCarts.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart == null) return 0;

        return await _context.ShoppingCartItems
            .AsNoTracking()
            .Where(ci => ci.CartID == cart.CartID)
            .SumAsync(ci => ci.Quantity);
    }

    private async Task<ShoppingCart> GetOrCreateCartAsync(string userId)
    {
        var cart = await _context.ShoppingCarts.FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart == null)
        {
            cart = new ShoppingCart
            {
                UserId = userId,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.ShoppingCarts.Add(cart);
            await _context.SaveChangesAsync();
        }
        return cart;
    }
}
