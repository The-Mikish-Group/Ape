using Ape.Data;
using Ape.Models;
using Ape.Models.ViewModels;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

namespace Ape.Services;

public class OrderService(
    ApplicationDbContext context,
    IShoppingCartService cartService,
    ISubscriptionService subscriptionService,
    ISystemSettingsService settingsService,
    IEmailSender emailSender,
    ILogger<OrderService> logger) : IOrderService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IShoppingCartService _cartService = cartService;
    private readonly ISubscriptionService _subscriptionService = subscriptionService;
    private readonly ISystemSettingsService _settingsService = settingsService;
    private readonly IEmailSender _emailSender = emailSender;
    private readonly ILogger<OrderService> _logger = logger;

    public async Task<(StoreOperationResult Result, int? OrderId)> CreateOrderFromCartAsync(string userId, int? shippingAddressId, string? customerNotes)
    {
        var cart = await _context.ShoppingCarts
            .Include(c => c.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null || !cart.Items.Any())
            return (StoreOperationResult.Failed("Cart is empty."), null);

        // Validate stock for physical items
        foreach (var item in cart.Items.Where(i => i.Product?.ProductType == ProductType.Physical && i.Product.TrackInventory))
        {
            if (item.Product!.StockQuantity < item.Quantity)
                return (StoreOperationResult.Failed($"'{item.Product.Name}' only has {item.Product.StockQuantity} in stock."), null);
        }

        var hasPhysical = cart.Items.Any(i => i.Product?.ProductType == ProductType.Physical);
        var hasDigital = cart.Items.Any(i => i.Product?.ProductType == ProductType.Digital);

        // Require shipping address for physical items
        ShippingAddress? address = null;
        if (hasPhysical)
        {
            if (!shippingAddressId.HasValue)
                return (StoreOperationResult.Failed("Shipping address is required for physical items."), null);

            address = await _context.ShippingAddresses.FirstOrDefaultAsync(a => a.AddressID == shippingAddressId && a.UserId == userId);
            if (address == null)
                return (StoreOperationResult.Failed("Shipping address not found."), null);
        }

        // Apply member pricing from current product data (not stored cart UnitPrice)
        var isMember = await _subscriptionService.HasActiveSubscriptionAsync(userId);
        decimal subtotal = 0;
        foreach (var item in cart.Items)
        {
            if (item.Product == null) continue;
            var price = isMember && item.Product.MemberPrice.HasValue && item.Product.MemberPrice < item.Product.Price
                ? item.Product.MemberPrice.Value
                : item.Product.Price;
            subtotal += price * item.Quantity;
        }

        // Calculate shipping
        decimal shippingCost = 0;
        if (hasPhysical)
        {
            var flatRate = await _settingsService.GetSettingAsync("Store__FlatRateShipping", "5.99");
            shippingCost = decimal.TryParse(flatRate, out var rate) ? rate : 5.99m;

            var freeThreshold = await _settingsService.GetSettingAsync("Store__FreeShippingThreshold", "");
            if (decimal.TryParse(freeThreshold, out var threshold) && threshold > 0 && subtotal >= threshold)
                shippingCost = 0;
        }

        var orderNumber = await GenerateOrderNumberAsync();

        var order = new Order
        {
            OrderNumber = orderNumber,
            UserId = userId,
            Status = OrderStatus.Pending,
            HasPhysicalItems = hasPhysical,
            HasDigitalItems = hasDigital,
            Subtotal = subtotal,
            ShippingCost = shippingCost,
            TaxAmount = 0,
            TotalAmount = subtotal + shippingCost,
            CustomerNotes = customerNotes?.Trim(),
            CustomerEmail = await GetUserEmailAsync(userId),
            OrderDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        if (address != null)
        {
            order.ShipToName = address.FullName;
            order.ShipToAddress1 = address.AddressLine1;
            order.ShipToAddress2 = address.AddressLine2;
            order.ShipToCity = address.City;
            order.ShipToState = address.State;
            order.ShipToZip = address.ZipCode;
            order.ShipToCountry = address.Country;
            order.ShipToPhone = address.Phone;
            order.ShippingMethod = "Flat Rate";
        }

        // Create order items and decrement stock
        foreach (var cartItem in cart.Items)
        {
            if (cartItem.Product == null)
            {
                _logger.LogWarning("Cart item {CartItemId} has null product (ProductID: {ProductId})", cartItem.CartItemID, cartItem.ProductID);
                continue;
            }

            var unitPrice = isMember && cartItem.Product.MemberPrice.HasValue && cartItem.Product.MemberPrice < cartItem.Product.Price
                ? cartItem.Product.MemberPrice.Value
                : cartItem.Product.Price;

            order.Items.Add(new OrderItem
            {
                ProductID = cartItem.ProductID,
                ProductName = cartItem.Product.Name,
                SKU = cartItem.Product.SKU,
                ProductType = cartItem.Product.ProductType,
                Quantity = cartItem.Quantity,
                UnitPrice = unitPrice,
                LineTotal = unitPrice * cartItem.Quantity
            });

            // Decrement stock for physical items
            if (cartItem.Product.ProductType == ProductType.Physical && cartItem.Product.TrackInventory)
            {
                cartItem.Product.StockQuantity -= cartItem.Quantity;
                if (cartItem.Product.StockQuantity < 0)
                    cartItem.Product.StockQuantity = 0;
            }
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Order {OrderNumber} created for user {UserId}. Total: {Total}", orderNumber, userId, order.TotalAmount);

        return (StoreOperationResult.Succeeded(order.OrderID, $"Order {orderNumber} created."), order.OrderID);
    }

    public async Task<StoreOperationResult> MarkOrderPaidAsync(int orderId, string gateway, string transactionId, string? paymentIntentId = null)
    {
        var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.OrderID == orderId);
        if (order == null)
            return StoreOperationResult.Failed("Order not found.");

        order.IsPaid = true;
        order.PaidDate = DateTime.UtcNow;
        order.PaymentGateway = gateway;
        order.PaymentTransactionId = transactionId;
        order.PaymentIntentId = paymentIntentId;

        // Digital-only orders go straight to Completed
        if (!order.HasPhysicalItems)
            order.Status = OrderStatus.Completed;
        else
            order.Status = OrderStatus.Processing;

        order.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Order {OrderNumber} marked as paid via {Gateway}. Transaction: {TransactionId}", order.OrderNumber, gateway, transactionId);

        // Send order confirmation email
        await SendOrderConfirmationEmailAsync(order);

        return StoreOperationResult.Succeeded(orderId, "Payment recorded.");
    }

    public async Task<OrderDetailViewModel?> GetOrderDetailAsync(int orderId)
    {
        return await BuildOrderDetailQuery()
            .Where(o => o.OrderID == orderId)
            .Select(o => MapToDetailViewModel(o))
            .FirstOrDefaultAsync();
    }

    public async Task<OrderDetailViewModel?> GetOrderDetailByNumberAsync(string orderNumber)
    {
        return await BuildOrderDetailQuery()
            .Where(o => o.OrderNumber == orderNumber)
            .Select(o => MapToDetailViewModel(o))
            .FirstOrDefaultAsync();
    }

    public async Task<List<OrderViewModel>> GetUserOrdersAsync(string userId, int page = 1, int pageSize = 20)
    {
        return await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => MapToViewModel(o))
            .ToListAsync();
    }

    public async Task<List<OrderViewModel>> GetAllOrdersAsync(OrderStatus? status = null, string? search = null, int page = 1, int pageSize = 25)
    {
        var query = _context.Orders.AsNoTracking().Include(o => o.Items).AsQueryable();

        if (status.HasValue)
            query = query.Where(o => o.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(o => o.OrderNumber.ToLower().Contains(term) ||
                                     (o.CustomerEmail != null && o.CustomerEmail.ToLower().Contains(term)));
        }

        return await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => MapToViewModel(o))
            .ToListAsync();
    }

    public async Task<int> GetOrderCountAsync(OrderStatus? status = null, string? search = null)
    {
        var query = _context.Orders.AsNoTracking().AsQueryable();
        if (status.HasValue) query = query.Where(o => o.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(o => o.OrderNumber.ToLower().Contains(term) || (o.CustomerEmail != null && o.CustomerEmail.ToLower().Contains(term)));
        }
        return await query.CountAsync();
    }

    public async Task<StoreOperationResult> UpdateOrderStatusAsync(int orderId, OrderStatus status)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) return StoreOperationResult.Failed("Order not found.");

        order.Status = status;
        order.UpdatedDate = DateTime.UtcNow;

        if (status == OrderStatus.Delivered) order.DeliveredDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Order {OrderNumber} status updated to {Status}", order.OrderNumber, status);
        return StoreOperationResult.SucceededNoId($"Order status updated to {status}.");
    }

    public async Task<StoreOperationResult> AddTrackingAsync(int orderId, string carrier, string trackingNumber)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) return StoreOperationResult.Failed("Order not found.");

        order.ShippingCarrier = carrier.Trim();
        order.TrackingNumber = trackingNumber.Trim();
        order.ShippedDate = DateTime.UtcNow;
        order.Status = OrderStatus.Shipped;
        order.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Tracking added to order {OrderNumber}: {Carrier} {TrackingNumber}", order.OrderNumber, carrier, trackingNumber);

        // Send shipping notification email
        await SendShippingNotificationEmailAsync(order);

        return StoreOperationResult.SucceededNoId("Tracking information added.");
    }

    public async Task<StoreOperationResult> UpdateAdminNotesAsync(int orderId, string notes)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) return StoreOperationResult.Failed("Order not found.");

        order.AdminNotes = notes.Trim();
        order.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return StoreOperationResult.SucceededNoId("Notes updated.");
    }

    public async Task<string> GenerateOrderNumberAsync()
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var todayCount = await _context.Orders
            .Where(o => o.OrderNumber.StartsWith($"ORD-{date}"))
            .CountAsync();

        return $"ORD-{date}-{(todayCount + 1):D4}";
    }

    // Dashboard
    public async Task<decimal> GetTotalRevenueAsync(DateTime? since = null)
    {
        var query = _context.Orders.AsNoTracking().Where(o => o.IsPaid);
        if (since.HasValue) query = query.Where(o => o.PaidDate >= since);
        return await query.SumAsync(o => o.TotalAmount);
    }

    public async Task<int> GetOrderCountByStatusAsync(OrderStatus status)
    {
        return await _context.Orders.AsNoTracking().CountAsync(o => o.Status == status);
    }

    public async Task<SalesReportViewModel> GetSalesReportAsync(DateTime startDate, DateTime endDate)
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.IsPaid && o.PaidDate >= startDate && o.PaidDate <= endDate)
            .ToListAsync();

        var allItems = orders.SelectMany(o => o.Items).ToList();

        var dailySales = orders
            .GroupBy(o => o.PaidDate!.Value.Date)
            .Select(g => new DailySalesData
            {
                Date = g.Key,
                OrderCount = g.Count(),
                Revenue = g.Sum(o => o.TotalAmount)
            })
            .OrderBy(d => d.Date)
            .ToList();

        return new SalesReportViewModel
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalRevenue = orders.Sum(o => o.TotalAmount),
            TotalOrders = orders.Count,
            AverageOrderValue = orders.Count > 0 ? orders.Average(o => o.TotalAmount) : 0,
            PhysicalSales = allItems.Where(i => i.ProductType == ProductType.Physical).Sum(i => i.Quantity),
            DigitalSales = allItems.Where(i => i.ProductType == ProductType.Digital).Sum(i => i.Quantity),
            SubscriptionSales = 0, // Subscriptions handled separately
            PhysicalRevenue = allItems.Where(i => i.ProductType == ProductType.Physical).Sum(i => i.LineTotal),
            DigitalRevenue = allItems.Where(i => i.ProductType == ProductType.Digital).Sum(i => i.LineTotal),
            SubscriptionRevenue = 0,
            DailySales = dailySales
        };
    }

    // Helpers
    private IQueryable<Order> BuildOrderDetailQuery()
    {
        return _context.Orders.AsNoTracking().Include(o => o.Items);
    }

    private static OrderViewModel MapToViewModel(Order o) => new()
    {
        OrderId = o.OrderID,
        OrderNumber = o.OrderNumber,
        Status = o.Status,
        OrderDate = o.OrderDate,
        TotalAmount = o.TotalAmount,
        ItemCount = o.Items.Sum(i => i.Quantity),
        HasPhysicalItems = o.HasPhysicalItems,
        HasDigitalItems = o.HasDigitalItems,
        IsPaid = o.IsPaid,
        PaymentGateway = o.PaymentGateway,
        TrackingNumber = o.TrackingNumber,
        ShippingCarrier = o.ShippingCarrier
    };

    private static OrderDetailViewModel MapToDetailViewModel(Order o) => new()
    {
        OrderId = o.OrderID,
        OrderNumber = o.OrderNumber,
        Status = o.Status,
        OrderDate = o.OrderDate,
        Subtotal = o.Subtotal,
        ShippingCost = o.ShippingCost,
        TaxAmount = o.TaxAmount,
        TotalAmount = o.TotalAmount,
        HasPhysicalItems = o.HasPhysicalItems,
        HasDigitalItems = o.HasDigitalItems,
        IsPaid = o.IsPaid,
        PaidDate = o.PaidDate,
        PaymentGateway = o.PaymentGateway,
        PaymentTransactionId = o.PaymentTransactionId,
        CustomerNotes = o.CustomerNotes,
        AdminNotes = o.AdminNotes,
        CustomerEmail = o.CustomerEmail,
        ShipToName = o.ShipToName,
        ShipToAddress = FormatShipToAddress(o),
        ShippingMethod = o.ShippingMethod,
        TrackingNumber = o.TrackingNumber,
        ShippingCarrier = o.ShippingCarrier,
        ShippedDate = o.ShippedDate,
        DeliveredDate = o.DeliveredDate,
        RefundTransactionId = o.RefundTransactionId,
        RefundedAmount = o.RefundedAmount,
        RefundedDate = o.RefundedDate,
        RefundReason = o.RefundReason,
        Items = o.Items.Select(i => new OrderItemViewModel
        {
            OrderItemId = i.OrderItemID,
            ProductName = i.ProductName,
            SKU = i.SKU,
            ProductType = i.ProductType,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            LineTotal = i.LineTotal
        }).ToList()
    };

    private static string? FormatShipToAddress(Order o)
    {
        if (string.IsNullOrEmpty(o.ShipToAddress1)) return null;
        var parts = new List<string> { o.ShipToAddress1 };
        if (!string.IsNullOrEmpty(o.ShipToAddress2)) parts.Add(o.ShipToAddress2);
        parts.Add($"{o.ShipToCity}, {o.ShipToState} {o.ShipToZip}");
        if (!string.IsNullOrEmpty(o.ShipToCountry)) parts.Add(o.ShipToCountry);
        return string.Join(", ", parts);
    }

    private async Task<string?> GetUserEmailAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.Email;
    }

    // ============================================================
    // Email Notifications
    // ============================================================

    private async Task SendOrderConfirmationEmailAsync(Order order)
    {
        try
        {
            var email = order.CustomerEmail ?? await GetUserEmailAsync(order.UserId);
            if (string.IsNullOrEmpty(email)) return;

            var itemRows = string.Join("", order.Items.Select(i =>
                $"<tr><td>{i.ProductName}</td><td>{i.Quantity}</td><td>{i.LineTotal:C}</td></tr>"));

            var html = $"""
                <h2>Order Confirmation</h2>
                <p>Thank you for your order! Here are the details:</p>
                <p><strong>Order Number:</strong> {order.OrderNumber}</p>
                <p><strong>Total:</strong> {order.TotalAmount:C}</p>
                <p><strong>Payment:</strong> {order.PaymentGateway}</p>
                <table border="1" cellpadding="8" cellspacing="0" style="border-collapse:collapse; width:100%;">
                    <tr style="background:#f0f0f0;"><th>Product</th><th>Qty</th><th>Total</th></tr>
                    {itemRows}
                </table>
                {(order.HasPhysicalItems ? "<p>Your physical items will be shipped soon. You'll receive tracking information via email.</p>" : "")}
                {(order.HasDigitalItems ? "<p>Your digital downloads are available in your order history.</p>" : "")}
                <p>Thank you for shopping with us!</p>
                """;

            await _emailSender.SendEmailAsync(email, $"Order Confirmation - {order.OrderNumber}", html);
            _logger.LogInformation("Order confirmation email sent for {OrderNumber} to {Email}", order.OrderNumber, email);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send order confirmation email for {OrderNumber}", order.OrderNumber);
        }
    }

    private async Task SendShippingNotificationEmailAsync(Order order)
    {
        try
        {
            var email = order.CustomerEmail ?? await GetUserEmailAsync(order.UserId);
            if (string.IsNullOrEmpty(email)) return;

            var html = $"""
                <h2>Your Order Has Shipped!</h2>
                <p>Great news! Your order <strong>{order.OrderNumber}</strong> has been shipped.</p>
                <p><strong>Carrier:</strong> {order.ShippingCarrier}</p>
                <p><strong>Tracking Number:</strong> {order.TrackingNumber}</p>
                <p>You can view your order details and track your shipment from your order history.</p>
                <p>Thank you for your purchase!</p>
                """;

            await _emailSender.SendEmailAsync(email, $"Shipping Update - {order.OrderNumber}", html);
            _logger.LogInformation("Shipping notification email sent for {OrderNumber} to {Email}", order.OrderNumber, email);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send shipping notification email for {OrderNumber}", order.OrderNumber);
        }
    }
}
