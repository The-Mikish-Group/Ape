using Ape.Models;

namespace Ape.Services;

public interface IStorePaymentService
{
    // Stripe one-time payments
    Task<PaymentResult> CreateStripePaymentIntentAsync(int orderId, string userId);
    Task<PaymentResult> ConfirmStripePaymentAsync(string paymentIntentId);

    // PayPal one-time payments
    Task<PaymentResult> CreatePayPalOrderAsync(int orderId, string userId, string returnUrl, string cancelUrl);
    Task<PaymentResult> CapturePayPalOrderAsync(string payPalOrderId, int orderId);

    // Stripe subscriptions
    Task<PaymentResult> CreateStripeSubscriptionAsync(string userId, string stripePriceId, int productId);
    Task<PaymentResult> CancelStripeSubscriptionAsync(string stripeSubscriptionId);

    // PayPal subscriptions
    Task<PaymentResult> CreatePayPalSubscriptionAsync(string userId, string paypalPlanId, int productId, string returnUrl, string cancelUrl);
    Task<PaymentResult> CancelPayPalSubscriptionAsync(string paypalSubscriptionId, string reason);

    // Refunds
    Task<PaymentResult> RefundStripePaymentAsync(string paymentIntentId, decimal? amount = null);
    Task<PaymentResult> RefundPayPalPaymentAsync(string captureId, decimal? amount = null);

    // Configuration checks
    Task<bool> IsStripeConfiguredAsync();
    Task<bool> IsPayPalConfiguredAsync();
    Task<string?> GetStripePublishableKeyAsync();
    Task<string?> GetPayPalClientIdAsync();
}
