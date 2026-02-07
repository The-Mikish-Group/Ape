using Ape.Data;
using Ape.Models;
using Ape.Models.PayPal;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace Ape.Services;

public class StorePaymentService(
    ApplicationDbContext context,
    SecureConfigurationService configService,
    ILogger<StorePaymentService> logger) : IStorePaymentService
{
    private readonly ApplicationDbContext _context = context;
    private readonly SecureConfigurationService _configService = configService;
    private readonly ILogger<StorePaymentService> _logger = logger;

    // ============================================================
    // Stripe One-Time
    // ============================================================

    public async Task<PaymentResult> CreateStripePaymentIntentAsync(int orderId, string userId)
    {
        try
        {
            var secretKey = await _configService.GetCredentialAsync("Stripe__SecretKey");
            if (string.IsNullOrWhiteSpace(secretKey))
                return PaymentResult.CreateFailure("Stripe is not configured.");

            StripeConfiguration.ApiKey = secretKey;

            var order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.OrderID == orderId && o.UserId == userId);
            if (order == null)
                return PaymentResult.CreateFailure("Order not found.");

            var paymentMethod = await GetOrCreateCustomerPaymentMethodAsync(userId);

            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(order.TotalAmount * 100),
                Currency = "usd",
                Metadata = new Dictionary<string, string>
                {
                    ["order_id"] = orderId.ToString(),
                    ["order_number"] = order.OrderNumber
                }
            };

            if (paymentMethod?.StripeCustomerId != null)
                options.Customer = paymentMethod.StripeCustomerId;

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            return new PaymentResult
            {
                Success = true,
                PaymentIntentId = paymentIntent.Id,
                ClientSecret = paymentIntent.ClientSecret
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe PaymentIntent creation failed for order {OrderId}", orderId);
            return PaymentResult.CreateFailure($"Payment failed: {ex.Message}");
        }
    }

    public async Task<PaymentResult> ConfirmStripePaymentAsync(string paymentIntentId)
    {
        try
        {
            var secretKey = await _configService.GetCredentialAsync("Stripe__SecretKey");
            if (string.IsNullOrWhiteSpace(secretKey))
                return PaymentResult.CreateFailure("Stripe is not configured.");

            StripeConfiguration.ApiKey = secretKey;

            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(paymentIntentId);

            if (paymentIntent.Status == "succeeded")
            {
                return new PaymentResult
                {
                    Success = true,
                    TransactionId = paymentIntent.Id,
                    PaymentIntentId = paymentIntent.Id
                };
            }

            return PaymentResult.CreateFailure($"Payment status: {paymentIntent.Status}");
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe payment confirmation failed for {PaymentIntentId}", paymentIntentId);
            return PaymentResult.CreateFailure($"Payment verification failed: {ex.Message}");
        }
    }

    // ============================================================
    // PayPal One-Time
    // ============================================================

    public async Task<PaymentResult> CreatePayPalOrderAsync(int orderId, string userId, string returnUrl, string cancelUrl)
    {
        try
        {
            var client = await CreatePayPalClientAsync();
            if (client == null)
                return PaymentResult.CreateFailure("PayPal is not configured.");

            var order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.OrderID == orderId && o.UserId == userId);
            if (order == null)
                return PaymentResult.CreateFailure("Order not found.");

            var ppOrder = await client.CreateOrderAsync(new PayPalOrderRequest
            {
                Intent = "CAPTURE",
                PurchaseUnits =
                [
                    new PayPalPurchaseUnit
                    {
                        ReferenceId = order.OrderNumber,
                        Description = $"Order {order.OrderNumber}",
                        CustomId = orderId.ToString(),
                        Amount = new PayPalAmount
                        {
                            CurrencyCode = "USD",
                            Value = order.TotalAmount.ToString("F2")
                        }
                    }
                ],
                ApplicationContext = new PayPalApplicationContext
                {
                    ReturnUrl = returnUrl,
                    CancelUrl = cancelUrl,
                    BrandName = "Store"
                }
            });

            var approvalUrl = ppOrder.Links.FirstOrDefault(l => l.Rel == "approve")?.Href;

            return new PaymentResult
            {
                Success = true,
                TransactionId = ppOrder.Id,
                ApprovalUrl = approvalUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayPal order creation failed for order {OrderId}", orderId);
            return PaymentResult.CreateFailure($"PayPal payment failed: {ex.Message}");
        }
    }

    public async Task<PaymentResult> CapturePayPalOrderAsync(string payPalOrderId, int orderId)
    {
        try
        {
            var client = await CreatePayPalClientAsync();
            if (client == null)
                return PaymentResult.CreateFailure("PayPal is not configured.");

            var capture = await client.CaptureOrderAsync(payPalOrderId);

            if (capture.Status == "COMPLETED")
            {
                var captureId = capture.PurchaseUnits.FirstOrDefault()?.Payments?.Captures.FirstOrDefault()?.Id;
                return new PaymentResult
                {
                    Success = true,
                    TransactionId = captureId ?? payPalOrderId
                };
            }

            return PaymentResult.CreateFailure($"PayPal capture status: {capture.Status}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayPal capture failed for PayPal order {PayPalOrderId}", payPalOrderId);
            return PaymentResult.CreateFailure($"PayPal capture failed: {ex.Message}");
        }
    }

    // ============================================================
    // Stripe Subscriptions
    // ============================================================

    public async Task<PaymentResult> CreateStripeSubscriptionAsync(string userId, string stripePriceId, int productId)
    {
        try
        {
            var secretKey = await _configService.GetCredentialAsync("Stripe__SecretKey");
            if (string.IsNullOrWhiteSpace(secretKey))
                return PaymentResult.CreateFailure("Stripe is not configured.");

            StripeConfiguration.ApiKey = secretKey;

            var paymentMethod = await GetOrCreateCustomerPaymentMethodAsync(userId);
            string customerId;

            if (paymentMethod?.StripeCustomerId != null)
            {
                customerId = paymentMethod.StripeCustomerId;
            }
            else
            {
                var userEmail = (await _context.Users.FindAsync(userId))?.Email ?? "customer@example.com";
                var customerService = new CustomerService();
                var customer = await customerService.CreateAsync(new CustomerCreateOptions
                {
                    Email = userEmail,
                    Metadata = new Dictionary<string, string> { ["user_id"] = userId }
                });
                customerId = customer.Id;

                if (paymentMethod != null)
                {
                    paymentMethod.StripeCustomerId = customerId;
                    paymentMethod.LastUpdated = DateTime.UtcNow;
                }
                else
                {
                    _context.CustomerPaymentMethods.Add(new CustomerPaymentMethod
                    {
                        UserId = userId,
                        StripeCustomerId = customerId,
                        PreferredGateway = "Stripe",
                        CreatedDate = DateTime.UtcNow,
                        LastUpdated = DateTime.UtcNow
                    });
                }
                await _context.SaveChangesAsync();
            }

            var stripeSubService = new Stripe.SubscriptionService();
            var subscription = await stripeSubService.CreateAsync(new SubscriptionCreateOptions
            {
                Customer = customerId,
                Items = [new SubscriptionItemOptions { Price = stripePriceId }],
                PaymentBehavior = "default_incomplete",
                PaymentSettings = new SubscriptionPaymentSettingsOptions
                {
                    SaveDefaultPaymentMethod = "on_subscription"
                },
                Metadata = new Dictionary<string, string>
                {
                    ["user_id"] = userId,
                    ["product_id"] = productId.ToString()
                },
                Expand = ["latest_invoice"]
            });

            // Fetch the invoice separately to get the payment intent client secret
            // (expanding from subscription would be 5 levels deep, exceeding Stripe's 4-level limit)
            string? clientSecret = null;
            if (subscription.LatestInvoice?.Id != null)
            {
                var invoiceService = new InvoiceService();
                var invoice = await invoiceService.GetAsync(subscription.LatestInvoice.Id, new InvoiceGetOptions
                {
                    Expand = ["payments.data.payment.payment_intent"]
                });
                clientSecret = invoice?.Payments?.Data?.FirstOrDefault()?.Payment?.PaymentIntent?.ClientSecret;
            }

            if (string.IsNullOrEmpty(clientSecret))
            {
                _logger.LogWarning("Stripe subscription {SubId} created but no client secret returned. Status: {Status}, Invoice: {InvoiceId}",
                    subscription.Id, subscription.Status, subscription.LatestInvoice?.Id);
                return PaymentResult.CreateFailure("Subscription created but payment confirmation is not available. Please contact support.");
            }

            return new PaymentResult
            {
                Success = true,
                SubscriptionId = subscription.Id,
                ClientSecret = clientSecret,
                CustomerId = customerId
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe subscription creation failed for user {UserId}", userId);
            return PaymentResult.CreateFailure($"Subscription failed: {ex.Message}");
        }
    }

    public async Task<PaymentResult> CancelStripeSubscriptionAsync(string stripeSubscriptionId)
    {
        try
        {
            var secretKey = await _configService.GetCredentialAsync("Stripe__SecretKey");
            if (string.IsNullOrWhiteSpace(secretKey))
                return PaymentResult.CreateFailure("Stripe is not configured.");

            StripeConfiguration.ApiKey = secretKey;

            var stripeSubService = new Stripe.SubscriptionService();
            var subscription = await stripeSubService.UpdateAsync(stripeSubscriptionId, new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = true
            });

            return new PaymentResult
            {
                Success = true,
                SubscriptionId = subscription.Id,
                CurrentPeriodEnd = subscription.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe subscription cancellation failed for {SubscriptionId}", stripeSubscriptionId);
            return PaymentResult.CreateFailure($"Cancellation failed: {ex.Message}");
        }
    }

    // ============================================================
    // PayPal Subscriptions
    // ============================================================

    public async Task<PaymentResult> CreatePayPalSubscriptionAsync(string userId, string paypalPlanId, int productId, string returnUrl, string cancelUrl)
    {
        try
        {
            var client = await CreatePayPalClientAsync();
            if (client == null)
                return PaymentResult.CreateFailure("PayPal is not configured.");

            var response = await client.CreateSubscriptionAsync(new PayPalSubscriptionRequest
            {
                PlanId = paypalPlanId,
                CustomId = $"{userId}|{productId}",
                ApplicationContext = new PayPalApplicationContext
                {
                    ReturnUrl = returnUrl,
                    CancelUrl = cancelUrl,
                    BrandName = "Store"
                }
            });

            var approvalUrl = response.Links.FirstOrDefault(l => l.Rel == "approve")?.Href;

            return new PaymentResult
            {
                Success = true,
                SubscriptionId = response.Id,
                ApprovalUrl = approvalUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayPal subscription creation failed for user {UserId}", userId);
            return PaymentResult.CreateFailure($"PayPal subscription failed: {ex.Message}");
        }
    }

    public async Task<PaymentResult> CancelPayPalSubscriptionAsync(string paypalSubscriptionId, string reason)
    {
        try
        {
            var client = await CreatePayPalClientAsync();
            if (client == null)
                return PaymentResult.CreateFailure("PayPal is not configured.");

            await client.CancelSubscriptionAsync(paypalSubscriptionId, reason);

            return PaymentResult.CreateSuccess(paypalSubscriptionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayPal subscription cancellation failed for {SubscriptionId}", paypalSubscriptionId);
            return PaymentResult.CreateFailure($"Cancellation failed: {ex.Message}");
        }
    }

    // ============================================================
    // Refunds
    // ============================================================

    public async Task<PaymentResult> RefundStripePaymentAsync(string paymentIntentId, decimal? amount = null)
    {
        try
        {
            var secretKey = await _configService.GetCredentialAsync("Stripe__SecretKey");
            if (string.IsNullOrWhiteSpace(secretKey))
                return PaymentResult.CreateFailure("Stripe is not configured.");

            StripeConfiguration.ApiKey = secretKey;

            var options = new RefundCreateOptions { PaymentIntent = paymentIntentId };
            if (amount.HasValue)
                options.Amount = (long)(amount.Value * 100);

            var service = new RefundService();
            var refund = await service.CreateAsync(options);

            return new PaymentResult
            {
                Success = refund.Status == "succeeded",
                TransactionId = refund.Id,
                ErrorMessage = refund.Status != "succeeded" ? $"Refund status: {refund.Status}" : null
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe refund failed for {PaymentIntentId}", paymentIntentId);
            return PaymentResult.CreateFailure($"Refund failed: {ex.Message}");
        }
    }

    public async Task<PaymentResult> RefundPayPalPaymentAsync(string captureId, decimal? amount = null)
    {
        try
        {
            var client = await CreatePayPalClientAsync();
            if (client == null)
                return PaymentResult.CreateFailure("PayPal is not configured.");

            var refund = await client.RefundCaptureAsync(captureId, amount);

            return new PaymentResult
            {
                Success = refund.Status == "COMPLETED",
                TransactionId = refund.Id,
                ErrorMessage = refund.Status != "COMPLETED" ? $"Refund status: {refund.Status}" : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayPal refund failed for capture {CaptureId}", captureId);
            return PaymentResult.CreateFailure($"Refund failed: {ex.Message}");
        }
    }

    // ============================================================
    // Configuration
    // ============================================================

    public async Task<bool> IsStripeConfiguredAsync()
    {
        var key = await _configService.GetCredentialAsync("Stripe__SecretKey");
        return !string.IsNullOrWhiteSpace(key);
    }

    public async Task<bool> IsPayPalConfiguredAsync()
    {
        var clientId = await _configService.GetCredentialAsync("PayPal__ClientId");
        var secret = await _configService.GetCredentialAsync("PayPal__ClientSecret");
        return !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(secret);
    }

    public async Task<string?> GetStripePublishableKeyAsync()
    {
        return await _configService.GetCredentialAsync("Stripe__PublishableKey");
    }

    public async Task<string?> GetPayPalClientIdAsync()
    {
        return await _configService.GetCredentialAsync("PayPal__ClientId");
    }

    // Helpers
    private async Task<CustomerPaymentMethod?> GetOrCreateCustomerPaymentMethodAsync(string userId)
    {
        return await _context.CustomerPaymentMethods.FirstOrDefaultAsync(pm => pm.UserId == userId);
    }

    private async Task<PayPalApiClient?> CreatePayPalClientAsync()
    {
        var clientId = await _configService.GetCredentialAsync("PayPal__ClientId");
        var clientSecret = await _configService.GetCredentialAsync("PayPal__ClientSecret");
        var mode = await _configService.GetCredentialAsync("PayPal__Mode") ?? "sandbox";

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            return null;

        return new PayPalApiClient(clientId, clientSecret, mode,
            LoggerFactory.Create(b => b.AddConsole()).CreateLogger<PayPalApiClient>());
    }
}
