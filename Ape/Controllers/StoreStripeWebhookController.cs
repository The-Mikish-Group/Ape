using Ape.Data;
using Ape.Models;
using Ape.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace Ape.Controllers;

[ApiController]
[Route("api/StoreStripeWebhook")]
public class StoreStripeWebhookController(
    ApplicationDbContext context,
    SecureConfigurationService configService,
    IOrderService orderService,
    IDigitalDeliveryService deliveryService,
    ISubscriptionService subscriptionService,
    ILogger<StoreStripeWebhookController> logger) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;
    private readonly SecureConfigurationService _configService = configService;
    private readonly IOrderService _orderService = orderService;
    private readonly IDigitalDeliveryService _deliveryService = deliveryService;
    private readonly ISubscriptionService _subscriptionService = subscriptionService;
    private readonly ILogger<StoreStripeWebhookController> _logger = logger;

    [HttpPost]
    public async Task<IActionResult> HandleWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        try
        {
            var webhookSecret = await _configService.GetCredentialAsync("Stripe__WebhookSecret");
            if (string.IsNullOrWhiteSpace(webhookSecret))
            {
                _logger.LogWarning("Stripe webhook secret not configured");
                return BadRequest("Webhook not configured");
            }

            var stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], webhookSecret);

            _logger.LogInformation("Received Stripe webhook: {EventType}", stripeEvent.Type);

            switch (stripeEvent.Type)
            {
                case EventTypes.PaymentIntentSucceeded:
                    await HandlePaymentIntentSucceeded(stripeEvent);
                    break;

                case EventTypes.PaymentIntentPaymentFailed:
                    await HandlePaymentIntentFailed(stripeEvent);
                    break;

                case EventTypes.InvoicePaid:
                    await HandleInvoicePaid(stripeEvent);
                    break;

                case EventTypes.CustomerSubscriptionUpdated:
                    await HandleSubscriptionUpdated(stripeEvent);
                    break;

                case EventTypes.CustomerSubscriptionDeleted:
                    await HandleSubscriptionDeleted(stripeEvent);
                    break;
            }

            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook signature verification failed");
            return BadRequest("Invalid signature");
        }
    }

    private async Task HandlePaymentIntentSucceeded(Event stripeEvent)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null) return;

        if (paymentIntent.Metadata.TryGetValue("order_id", out var orderIdStr) && int.TryParse(orderIdStr, out var orderId))
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null && !order.IsPaid)
            {
                await _orderService.MarkOrderPaidAsync(orderId, "Stripe", paymentIntent.Id, paymentIntent.Id);
                await _deliveryService.CreateDownloadRecordsForOrderAsync(orderId);
                _logger.LogInformation("Webhook: Order {OrderId} marked paid via Stripe PaymentIntent", orderId);
            }
        }
    }

    private async Task HandlePaymentIntentFailed(Event stripeEvent)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null) return;

        if (paymentIntent.Metadata.TryGetValue("order_id", out var orderIdStr) && int.TryParse(orderIdStr, out var orderId))
        {
            _logger.LogWarning("Webhook: Payment failed for order {OrderId}. Reason: {Reason}", orderId, paymentIntent.LastPaymentError?.Message);
        }
    }

    private async Task HandleInvoicePaid(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        var subscriptionId = invoice?.Parent?.SubscriptionDetails?.SubscriptionId;
        if (subscriptionId == null) return;

        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId);

        if (subscription != null)
        {
            subscription.Status = SubscriptionStatus.Active;
            subscription.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Record payment history
            var amount = invoice!.AmountPaid > 0 ? invoice.AmountPaid / 100m : subscription.Amount;
            var transactionId = invoice.Id ?? "unknown";
            await _subscriptionService.RecordPaymentAsync(
                subscription.SubscriptionID, amount, "Stripe", transactionId, "Renewal");

            _logger.LogInformation("Webhook: Subscription {SubscriptionId} renewed via invoice", subscription.SubscriptionID);
        }
    }

    private async Task HandleSubscriptionUpdated(Event stripeEvent)
    {
        var stripeSub = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSub == null) return;

        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSub.Id);

        if (subscription != null)
        {
            subscription.Status = stripeSub.Status switch
            {
                "active" => SubscriptionStatus.Active,
                "past_due" => SubscriptionStatus.PastDue,
                "canceled" => SubscriptionStatus.Cancelled,
                _ => subscription.Status
            };
            var firstItem = stripeSub.Items?.Data?.FirstOrDefault();
            subscription.CurrentPeriodStart = firstItem?.CurrentPeriodStart;
            subscription.CurrentPeriodEnd = firstItem?.CurrentPeriodEnd;
            subscription.UpdatedDate = DateTime.UtcNow;

            if (stripeSub.CancelAtPeriodEnd)
            {
                subscription.CancelledDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Webhook: Subscription {SubscriptionId} updated to {Status}", subscription.SubscriptionID, subscription.Status);
        }
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent)
    {
        var stripeSub = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSub == null) return;

        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSub.Id);

        if (subscription != null)
        {
            subscription.Status = SubscriptionStatus.Cancelled;
            subscription.CancelledDate ??= DateTime.UtcNow;
            subscription.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Webhook: Subscription {SubscriptionId} deleted/cancelled", subscription.SubscriptionID);
        }
    }
}
