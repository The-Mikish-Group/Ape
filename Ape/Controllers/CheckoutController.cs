using System.Security.Claims;
using Ape.Models.ViewModels;
using Ape.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ape.Controllers;

[Authorize]
public class CheckoutController(
    IShoppingCartService cartService,
    IShippingAddressService addressService,
    IOrderService orderService,
    IStorePaymentService paymentService,
    IDigitalDeliveryService deliveryService,
    ILogger<CheckoutController> logger) : Controller
{
    private readonly IShoppingCartService _cartService = cartService;
    private readonly IShippingAddressService _addressService = addressService;
    private readonly IOrderService _orderService = orderService;
    private readonly IStorePaymentService _paymentService = paymentService;
    private readonly IDigitalDeliveryService _deliveryService = deliveryService;
    private readonly ILogger<CheckoutController> _logger = logger;

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var cart = await _cartService.GetCartAsync(userId);

        if (!cart.Items.Any())
        {
            TempData["ErrorMessage"] = "Your cart is empty.";
            return RedirectToAction("Index", "Cart");
        }

        if (cart.HasOutOfStockItems)
        {
            TempData["ErrorMessage"] = "Please remove out-of-stock items before checkout.";
            return RedirectToAction("Index", "Cart");
        }

        var addresses = await _addressService.GetAddressesAsync(userId);
        var defaultAddress = addresses.FirstOrDefault(a => a.IsDefault);

        var viewModel = new CheckoutViewModel
        {
            Cart = cart,
            Addresses = addresses,
            SelectedAddressId = defaultAddress?.AddressId,
            RequiresShipping = cart.HasPhysicalItems
        };

        ViewData["Title"] = "Checkout";
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(int? shippingAddressId, string? customerNotes)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var (result, orderId) = await _orderService.CreateOrderFromCartAsync(userId, shippingAddressId, customerNotes);

        if (!result.Success || !orderId.HasValue)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(Payment), new { orderId = orderId.Value });
    }

    public async Task<IActionResult> Payment(int orderId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var order = await _orderService.GetOrderDetailAsync(orderId);

        if (order == null || order.IsPaid)
        {
            TempData["ErrorMessage"] = "Order not found or already paid.";
            return RedirectToAction("Index", "OrderHistory");
        }

        var stripeEnabled = await _paymentService.IsStripeConfiguredAsync();
        var paypalEnabled = await _paymentService.IsPayPalConfiguredAsync();
        string? clientSecret = null;

        if (stripeEnabled)
        {
            var stripeResult = await _paymentService.CreateStripePaymentIntentAsync(orderId, userId);
            if (stripeResult.Success)
                clientSecret = stripeResult.ClientSecret;
        }

        var viewModel = new PaymentViewModel
        {
            OrderNumber = order.OrderNumber,
            OrderId = orderId,
            TotalAmount = order.TotalAmount,
            StripePublishableKey = stripeEnabled ? await _paymentService.GetStripePublishableKeyAsync() : null,
            PayPalClientId = paypalEnabled ? await _paymentService.GetPayPalClientIdAsync() : null,
            ClientSecret = clientSecret,
            StripeEnabled = stripeEnabled,
            PayPalEnabled = paypalEnabled
        };

        ViewData["Title"] = "Payment";
        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmStripePayment([FromBody] ConfirmPaymentRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var result = await _paymentService.ConfirmStripePaymentAsync(request.PaymentIntentId);
        if (!result.Success)
            return Json(new { success = false, message = result.ErrorMessage });

        await _orderService.MarkOrderPaidAsync(request.OrderId, "Stripe", result.TransactionId!, result.PaymentIntentId);
        await _deliveryService.CreateDownloadRecordsForOrderAsync(request.OrderId);
        await _cartService.ClearCartAsync(userId);

        var order = await _orderService.GetOrderDetailAsync(request.OrderId);
        return Json(new { success = true, orderNumber = order?.OrderNumber });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePayPalOrder(int orderId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var returnUrl = Url.Action(nameof(PayPalReturn), "Checkout", null, Request.Scheme)!;
        var cancelUrl = Url.Action(nameof(Payment), "Checkout", new { orderId }, Request.Scheme)!;

        var result = await _paymentService.CreatePayPalOrderAsync(orderId, userId, returnUrl, cancelUrl);

        if (!result.Success)
            return Json(new { success = false, message = result.ErrorMessage });

        return Json(new { success = true, paypalOrderId = result.TransactionId });
    }

    [HttpPost]
    public async Task<IActionResult> CapturePayPalOrder([FromBody] CapturePayPalRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var result = await _paymentService.CapturePayPalOrderAsync(request.PayPalOrderId, request.OrderId);
        if (!result.Success)
            return Json(new { success = false, message = result.ErrorMessage });

        await _orderService.MarkOrderPaidAsync(request.OrderId, "PayPal", result.TransactionId!);
        await _deliveryService.CreateDownloadRecordsForOrderAsync(request.OrderId);
        await _cartService.ClearCartAsync(userId);

        var order = await _orderService.GetOrderDetailAsync(request.OrderId);
        return Json(new { success = true, orderNumber = order?.OrderNumber });
    }

    public IActionResult PayPalReturn(string token)
    {
        // PayPal redirect-based return (fallback)
        return RedirectToAction("Index", "OrderHistory");
    }

    public async Task<IActionResult> Confirmation(string orderNumber)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var order = await _orderService.GetOrderDetailByNumberAsync(orderNumber);

        if (order == null)
        {
            TempData["ErrorMessage"] = "Order not found.";
            return RedirectToAction("Index", "OrderHistory");
        }

        var downloads = order.HasDigitalItems
            ? await _deliveryService.GetDownloadsForOrderAsync(order.OrderId, userId)
            : [];

        var viewModel = new OrderConfirmationViewModel
        {
            OrderNumber = order.OrderNumber,
            TotalAmount = order.TotalAmount,
            PaymentGateway = order.PaymentGateway ?? "Unknown",
            HasDigitalItems = order.HasDigitalItems,
            HasPhysicalItems = order.HasPhysicalItems,
            Items = order.Items,
            Downloads = downloads
        };

        ViewData["Title"] = "Order Confirmation";
        return View(viewModel);
    }
}

public class ConfirmPaymentRequest
{
    public int OrderId { get; set; }
    public string PaymentIntentId { get; set; } = string.Empty;
}

public class CapturePayPalRequest
{
    public int OrderId { get; set; }
    public string PayPalOrderId { get; set; } = string.Empty;
}
