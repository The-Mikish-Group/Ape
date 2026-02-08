using Ape.Models;
using Ape.Models.ViewModels;

namespace Ape.Services;

public interface ISubscriptionService
{
    Task<bool> HasActiveSubscriptionAsync(string userId);
    Task<Models.Subscription?> GetActiveSubscriptionAsync(string userId);
    Task<SubscriptionDetailViewModel?> GetSubscriptionDetailAsync(string userId);
    Task<StoreOperationResult> CreateSubscriptionAsync(string userId, int productId, string gateway, string gatewaySubscriptionId, decimal amount, string? billingInterval);
    Task<StoreOperationResult> CancelSubscriptionAsync(string userId, string? reason);
    Task<List<SubscriptionListViewModel>> GetAllSubscriptionsAsync(SubscriptionStatus? status = null, int page = 1, int pageSize = 25);
    Task<int> GetSubscriptionCountAsync(SubscriptionStatus? status = null);
    Task<int> GetActiveSubscriptionCountAsync();
    Task<AdminSubscriptionDetailViewModel?> GetSubscriptionDetailByIdAsync(int subscriptionId);
    Task<StoreOperationResult> RecordPaymentAsync(int subscriptionId, decimal amount, string gateway, string transactionId, string? notes);
    Task<SubscriptionPayment?> GetPaymentByIdAsync(int paymentId);
    Task<StoreOperationResult> MarkPaymentRefundedAsync(int paymentId, string refundTransactionId, string? reason);
}
