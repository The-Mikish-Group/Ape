using System.Security.Claims;
using Ape.Models.ViewModels;
using Ape.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ape.Controllers;

[Authorize]
public class CartController(
    IShoppingCartService cartService,
    ILogger<CartController> logger) : Controller
{
    private readonly IShoppingCartService _cartService = cartService;
    private readonly ILogger<CartController> _logger = logger;

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var cart = await _cartService.GetCartAsync(userId);

        ViewData["Title"] = "Shopping Cart";
        return View(cart);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddToCartRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Json(new { success = false, message = "Please log in to add items to your cart." });

        var isMember = false; // Will integrate with ISubscriptionService in Phase 1E
        var result = await _cartService.AddItemAsync(userId, request.ProductId, request.Quantity, isMember);

        return Json(new { success = result.Success, message = result.Message, cartCount = result.EntityId ?? 0 });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateQuantity([FromBody] UpdateCartQuantityRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _cartService.UpdateQuantityAsync(userId, request.CartItemId, request.Quantity);

        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost]
    public async Task<IActionResult> Remove([FromBody] RemoveFromCartRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _cartService.RemoveItemAsync(userId, request.CartItemId);

        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpGet]
    public async Task<IActionResult> GetCartCount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Json(new { count = 0 });

        var count = await _cartService.GetCartItemCountAsync(userId);
        return Json(new { count });
    }
}
