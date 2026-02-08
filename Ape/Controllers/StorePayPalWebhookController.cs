using System.Text.Json;
using Ape.Data;
using Ape.Models;
using Ape.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ape.Controllers;

[ApiController]
[Route("api/store/paypal-webhook")]
public class StorePayPalWebhookController(
    ApplicationDbContext context,
    IOrderService orderService,
    IDigitalDeliveryService deliveryService,
    ISubscriptionService subscriptionService,
    ILogger<StorePayPalWebhookController> logger) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;
    private readonly IOrderService _orderService = orderService;
    private readonly IDigitalDeliveryService _deliveryService = deliveryService;
    private readonly ISubscriptionService _subscriptionService = subscriptionService;
    private readonly ILogger<StorePayPalWebhookController> _logger = logger;

    [HttpPost]
    public async Task<IActionResult> HandleWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        try
        {
            var webhook = JsonSerializer.Deserialize<Models.PayPal.PayPalWebhookEvent>(json);
            if (webhook == null) return BadRequest();

            _logger.LogInformation("Received PayPal webhook: {EventType}", webhook.EventType);

            switch (webhook.EventType)
            {
                case "PAYMENT.CAPTURE.COMPLETED":
                    await HandlePaymentCaptureCompleted(webhook);
                    break;

                case "BILLING.SUBSCRIPTION.ACTIVATED":
                    await HandleSubscriptionActivated(webhook);
                    break;

                case "BILLING.SUBSCRIPTION.CANCELLED":
                    await HandleSubscriptionCancelled(webhook);
                    break;

                case "BILLING.SUBSCRIPTION.EXPIRED":
                    await HandleSubscriptionExpired(webhook);
                    break;

                case "BILLING.SUBSCRIPTION.PAYMENT.FAILED":
                    await HandleSubscriptionPaymentFailed(webhook);
                    break;
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayPal webhook processing failed");
            return StatusCode(500);
        }
    }

    private async Task HandlePaymentCaptureCompleted(Models.PayPal.PayPalWebhookEvent webhook)
    {
        var resource = webhook.Resource;
        var captureId = resource.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        var customId = resource.TryGetProperty("custom_id", out var customProp) ? customProp.GetString() : null;

        if (captureId == null) return;

        // Check if this is for a store order
        if (int.TryParse(customId, out var orderId))
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null && !order.IsPaid)
            {
                await _orderService.MarkOrderPaidAsync(orderId, "PayPal", captureId);
                await _deliveryService.CreateDownloadRecordsForOrderAsync(orderId);
                _logger.LogInformation("PayPal Webhook: Order {OrderId} marked paid. Capture: {CaptureId}", orderId, captureId);
            }
        }
    }

    private async Task HandleSubscriptionActivated(Models.PayPal.PayPalWebhookEvent webhook)
    {
        var resource = webhook.Resource;
        var subscriptionId = resource.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

        if (subscriptionId == null) return;

        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.PayPalSubscriptionId == subscriptionId);

        if (subscription != null)
        {
            subscription.Status = SubscriptionStatus.Active;
            subscription.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Record initial payment
            await _subscriptionService.RecordPaymentAsync(
                subscription.SubscriptionID, subscription.Amount, "PayPal", subscriptionId, "Initial payment");

            _logger.LogInformation("PayPal Webhook: Subscription {SubscriptionId} activated", subscriptionId);
        }
    }

    private async Task HandleSubscriptionCancelled(Models.PayPal.PayPalWebhookEvent webhook)
    {
        var resource = webhook.Resource;
        var subscriptionId = resource.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

        if (subscriptionId == null) return;

        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.PayPalSubscriptionId == subscriptionId);

        if (subscription != null)
        {
            subscription.Status = SubscriptionStatus.Cancelled;
            subscription.CancelledDate ??= DateTime.UtcNow;
            subscription.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("PayPal Webhook: Subscription {SubscriptionId} cancelled", subscriptionId);
        }
    }

    private async Task HandleSubscriptionExpired(Models.PayPal.PayPalWebhookEvent webhook)
    {
        var resource = webhook.Resource;
        var subscriptionId = resource.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

        if (subscriptionId == null) return;

        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.PayPalSubscriptionId == subscriptionId);

        if (subscription != null)
        {
            subscription.Status = SubscriptionStatus.Expired;
            subscription.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("PayPal Webhook: Subscription {SubscriptionId} expired", subscriptionId);
        }
    }

    private async Task HandleSubscriptionPaymentFailed(Models.PayPal.PayPalWebhookEvent webhook)
    {
        var resource = webhook.Resource;
        var subscriptionId = resource.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

        if (subscriptionId == null) return;

        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.PayPalSubscriptionId == subscriptionId);

        if (subscription != null)
        {
            subscription.Status = SubscriptionStatus.PastDue;
            subscription.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogWarning("PayPal Webhook: Subscription {SubscriptionId} payment failed", subscriptionId);
        }
    }
}
