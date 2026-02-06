using System.Security.Claims;
using Ape.Models;
using Ape.Models.ViewModels;
using Ape.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ape.Controllers;

[Authorize]
public class SubscriptionController(
    ISubscriptionService subscriptionService,
    IStorePaymentService paymentService,
    IProductCatalogService catalogService,
    ILogger<SubscriptionController> logger) : Controller
{
    private readonly ISubscriptionService _subscriptionService = subscriptionService;
    private readonly IStorePaymentService _paymentService = paymentService;
    private readonly IProductCatalogService _catalogService = catalogService;
    private readonly ILogger<SubscriptionController> _logger = logger;

    public async Task<IActionResult> Subscribe(int productId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Check for existing subscription
        var existing = await _subscriptionService.GetActiveSubscriptionAsync(userId);
        if (existing != null)
        {
            TempData["ErrorMessage"] = "You already have an active subscription.";
            return RedirectToAction(nameof(Manage));
        }

        var product = await _catalogService.GetProductByIdAsync(productId);
        if (product == null || product.ProductType != ProductType.Subscription)
        {
            TempData["ErrorMessage"] = "Subscription product not found.";
            return RedirectToAction("Browse", "Store");
        }

        var stripeEnabled = await _paymentService.IsStripeConfiguredAsync() && !string.IsNullOrEmpty(product.StripePriceId);
        var paypalEnabled = await _paymentService.IsPayPalConfiguredAsync() && !string.IsNullOrEmpty(product.PayPalPlanId);

        string? stripeClientSecret = null;
        string? stripeSubscriptionId = null;

        if (stripeEnabled)
        {
            var result = await _paymentService.CreateStripeSubscriptionAsync(userId, product.StripePriceId!, productId);
            if (result.Success)
            {
                stripeClientSecret = result.ClientSecret;
                stripeSubscriptionId = result.SubscriptionId;
            }
        }

        var viewModel = new SubscribeViewModel
        {
            Product = product,
            StripeEnabled = stripeEnabled,
            PayPalEnabled = paypalEnabled,
            StripePublishableKey = stripeEnabled ? await _paymentService.GetStripePublishableKeyAsync() : null,
            PayPalClientId = paypalEnabled ? await _paymentService.GetPayPalClientIdAsync() : null,
            StripeClientSecret = stripeClientSecret,
            StripeSubscriptionId = stripeSubscriptionId,
            PayPalPlanId = product.PayPalPlanId
        };

        ViewData["Title"] = $"Subscribe: {product.Name}";
        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmStripeSubscription([FromBody] ConfirmStripeSubRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var product = await _catalogService.GetProductByIdAsync(request.ProductId);
        if (product == null)
            return Json(new { success = false, message = "Product not found." });

        var result = await _subscriptionService.CreateSubscriptionAsync(
            userId, request.ProductId, "Stripe", request.StripeSubscriptionId,
            product.Price, product.BillingInterval);

        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePayPalSubscription(int productId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var product = await _catalogService.GetProductByIdAsync(productId);
        if (product == null || string.IsNullOrEmpty(product.PayPalPlanId))
            return Json(new { success = false, message = "Product not configured for PayPal." });

        var returnUrl = Url.Action(nameof(PayPalSubscriptionReturn), "Subscription", new { productId }, Request.Scheme)!;
        var cancelUrl = Url.Action(nameof(Subscribe), "Subscription", new { productId }, Request.Scheme)!;

        var result = await _paymentService.CreatePayPalSubscriptionAsync(
            userId, product.PayPalPlanId, productId, returnUrl, cancelUrl);

        if (!result.Success)
            return Json(new { success = false, message = result.ErrorMessage });

        return Json(new { success = true, approvalUrl = result.ApprovalUrl });
    }

    public async Task<IActionResult> PayPalSubscriptionReturn(int productId, string subscription_id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var product = await _catalogService.GetProductByIdAsync(productId);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Product not found.";
            return RedirectToAction("Browse", "Store");
        }

        var result = await _subscriptionService.CreateSubscriptionAsync(
            userId, productId, "PayPal", subscription_id,
            product.Price, product.BillingInterval);

        if (result.Success)
        {
            TempData["SuccessMessage"] = "Your subscription is now active!";
            return RedirectToAction(nameof(Manage));
        }

        TempData["ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Subscribe), new { productId });
    }

    public async Task<IActionResult> Manage()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var subscription = await _subscriptionService.GetSubscriptionDetailAsync(userId);

        if (subscription == null)
        {
            ViewData["Title"] = "My Subscription";
            return View("NoSubscription");
        }

        ViewData["Title"] = "Manage Subscription";
        return View(subscription);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(string? reason)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Cancel at gateway first
        var activeSub = await _subscriptionService.GetActiveSubscriptionAsync(userId);
        if (activeSub != null)
        {
            if (!string.IsNullOrEmpty(activeSub.StripeSubscriptionId))
            {
                await _paymentService.CancelStripeSubscriptionAsync(activeSub.StripeSubscriptionId);
            }
            else if (!string.IsNullOrEmpty(activeSub.PayPalSubscriptionId))
            {
                await _paymentService.CancelPayPalSubscriptionAsync(activeSub.PayPalSubscriptionId, reason ?? "Customer requested cancellation");
            }
        }

        var result = await _subscriptionService.CancelSubscriptionAsync(userId, reason);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Cancelled));
        }

        TempData["ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Manage));
    }

    public IActionResult Cancelled()
    {
        ViewData["Title"] = "Subscription Cancelled";
        return View();
    }
}

public class ConfirmStripeSubRequest
{
    public int ProductId { get; set; }
    public string StripeSubscriptionId { get; set; } = string.Empty;
}
