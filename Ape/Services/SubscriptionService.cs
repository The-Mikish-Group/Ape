using Ape.Data;
using Ape.Models;
using Ape.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Ape.Services;

public class SubscriptionService(
    ApplicationDbContext context,
    ILogger<SubscriptionService> logger) : ISubscriptionService
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<SubscriptionService> _logger = logger;

    public async Task<bool> HasActiveSubscriptionAsync(string userId)
    {
        return await _context.Subscriptions
            .AsNoTracking()
            .AnyAsync(s => s.UserId == userId && s.Status == SubscriptionStatus.Active);
    }

    public async Task<Subscription?> GetActiveSubscriptionAsync(string userId)
    {
        return await _context.Subscriptions
            .AsNoTracking()
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == SubscriptionStatus.Active);
    }

    public async Task<SubscriptionDetailViewModel?> GetSubscriptionDetailAsync(string userId)
    {
        var sub = await _context.Subscriptions
            .AsNoTracking()
            .Include(s => s.Product)
            .Where(s => s.UserId == userId && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.PastDue))
            .FirstOrDefaultAsync();

        if (sub == null) return null;

        return new SubscriptionDetailViewModel
        {
            SubscriptionId = sub.SubscriptionID,
            ProductName = sub.Product?.Name ?? "Unknown",
            ProductDescription = sub.Product?.ShortDescription,
            Status = sub.Status,
            PaymentGateway = sub.PaymentGateway,
            Amount = sub.Amount,
            BillingInterval = sub.BillingInterval,
            CurrentPeriodStart = sub.CurrentPeriodStart,
            CurrentPeriodEnd = sub.CurrentPeriodEnd,
            CancelledDate = sub.CancelledDate,
            CancelReason = sub.CancelReason,
            CreatedDate = sub.CreatedDate
        };
    }

    public async Task<StoreOperationResult> CreateSubscriptionAsync(
        string userId, int productId, string gateway,
        string gatewaySubscriptionId, decimal amount, string? billingInterval)
    {
        // Check for existing active subscription
        var existing = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == SubscriptionStatus.Active);

        if (existing != null)
            return StoreOperationResult.Failed("You already have an active subscription.");

        var subscription = new Subscription
        {
            UserId = userId,
            ProductID = productId,
            Status = SubscriptionStatus.Active,
            PaymentGateway = gateway,
            Amount = amount,
            BillingInterval = billingInterval,
            CurrentPeriodStart = DateTime.UtcNow,
            CurrentPeriodEnd = billingInterval == "year"
                ? DateTime.UtcNow.AddYears(1)
                : DateTime.UtcNow.AddMonths(1),
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        if (gateway == "Stripe")
            subscription.StripeSubscriptionId = gatewaySubscriptionId;
        else if (gateway == "PayPal")
            subscription.PayPalSubscriptionId = gatewaySubscriptionId;

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Subscription created for user {UserId}, product {ProductId}, gateway {Gateway}",
            userId, productId, gateway);

        return StoreOperationResult.Succeeded(subscription.SubscriptionID, "Subscription activated successfully!");
    }

    public async Task<StoreOperationResult> CancelSubscriptionAsync(string userId, string? reason)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == SubscriptionStatus.Active);

        if (subscription == null)
            return StoreOperationResult.Failed("No active subscription found.");

        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.CancelledDate = DateTime.UtcNow;
        subscription.CancelReason = reason;
        subscription.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Subscription {SubscriptionId} cancelled for user {UserId}", subscription.SubscriptionID, userId);
        return StoreOperationResult.SucceededNoId("Your subscription has been cancelled.");
    }

    public async Task<List<SubscriptionListViewModel>> GetAllSubscriptionsAsync(
        SubscriptionStatus? status = null, int page = 1, int pageSize = 25)
    {
        var query = _context.Subscriptions
            .AsNoTracking()
            .Include(s => s.Product)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        return await query
            .OrderByDescending(s => s.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SubscriptionListViewModel
            {
                SubscriptionId = s.SubscriptionID,
                UserEmail = s.UserId,
                ProductName = s.Product != null ? s.Product.Name : "Unknown",
                Status = s.Status,
                PaymentGateway = s.PaymentGateway,
                Amount = s.Amount,
                BillingInterval = s.BillingInterval,
                CurrentPeriodEnd = s.CurrentPeriodEnd,
                CreatedDate = s.CreatedDate
            })
            .ToListAsync();
    }

    public async Task<int> GetSubscriptionCountAsync(SubscriptionStatus? status = null)
    {
        var query = _context.Subscriptions.AsNoTracking().AsQueryable();

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        return await query.CountAsync();
    }

    public async Task<int> GetActiveSubscriptionCountAsync()
    {
        return await _context.Subscriptions
            .AsNoTracking()
            .CountAsync(s => s.Status == SubscriptionStatus.Active);
    }

    public async Task<AdminSubscriptionDetailViewModel?> GetSubscriptionDetailByIdAsync(int subscriptionId)
    {
        var sub = await _context.Subscriptions
            .AsNoTracking()
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.SubscriptionID == subscriptionId);

        if (sub == null) return null;

        // Look up user email from AspNetUsers
        var userEmail = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == sub.UserId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync() ?? sub.UserId;

        var payments = await _context.SubscriptionPayments
            .AsNoTracking()
            .Where(p => p.SubscriptionID == subscriptionId)
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new SubscriptionPaymentViewModel
            {
                PaymentID = p.PaymentID,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                PaymentGateway = p.PaymentGateway,
                TransactionId = p.TransactionId,
                Status = p.Status,
                RefundTransactionId = p.RefundTransactionId,
                RefundedDate = p.RefundedDate,
                RefundReason = p.RefundReason,
                Notes = p.Notes
            })
            .ToListAsync();

        return new AdminSubscriptionDetailViewModel
        {
            SubscriptionId = sub.SubscriptionID,
            UserId = sub.UserId,
            UserEmail = userEmail,
            ProductName = sub.Product?.Name ?? "Unknown",
            ProductDescription = sub.Product?.ShortDescription,
            Status = sub.Status,
            PaymentGateway = sub.PaymentGateway,
            Amount = sub.Amount,
            BillingInterval = sub.BillingInterval,
            StripeSubscriptionId = sub.StripeSubscriptionId,
            PayPalSubscriptionId = sub.PayPalSubscriptionId,
            CurrentPeriodStart = sub.CurrentPeriodStart,
            CurrentPeriodEnd = sub.CurrentPeriodEnd,
            CancelledDate = sub.CancelledDate,
            CancelReason = sub.CancelReason,
            CreatedDate = sub.CreatedDate,
            Payments = payments
        };
    }

    public async Task<StoreOperationResult> RecordPaymentAsync(
        int subscriptionId, decimal amount, string gateway, string transactionId, string? notes)
    {
        // Avoid duplicate records for the same transaction
        var exists = await _context.SubscriptionPayments
            .AnyAsync(p => p.TransactionId == transactionId);

        if (exists)
        {
            _logger.LogInformation("Payment already recorded for transaction {TransactionId}", transactionId);
            return StoreOperationResult.SucceededNoId("Payment already recorded.");
        }

        var payment = new SubscriptionPayment
        {
            SubscriptionID = subscriptionId,
            Amount = amount,
            PaymentDate = DateTime.UtcNow,
            PaymentGateway = gateway,
            TransactionId = transactionId,
            Status = "Paid",
            Notes = notes,
            CreatedDate = DateTime.UtcNow
        };

        _context.SubscriptionPayments.Add(payment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Payment recorded for subscription {SubscriptionId}: {Amount:C} via {Gateway}",
            subscriptionId, amount, gateway);

        return StoreOperationResult.Succeeded(payment.PaymentID, "Payment recorded.");
    }

    public async Task<SubscriptionPayment?> GetPaymentByIdAsync(int paymentId)
    {
        return await _context.SubscriptionPayments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PaymentID == paymentId);
    }

    public async Task<StoreOperationResult> MarkPaymentRefundedAsync(
        int paymentId, string refundTransactionId, string? reason)
    {
        var payment = await _context.SubscriptionPayments
            .FirstOrDefaultAsync(p => p.PaymentID == paymentId);

        if (payment == null)
            return StoreOperationResult.Failed("Payment record not found.");

        if (payment.Status == "Refunded")
            return StoreOperationResult.Failed("Payment has already been refunded.");

        payment.Status = "Refunded";
        payment.RefundTransactionId = refundTransactionId;
        payment.RefundedDate = DateTime.UtcNow;
        payment.RefundReason = reason;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Payment {PaymentId} marked as refunded. Refund transaction: {RefundTransactionId}",
            paymentId, refundTransactionId);

        return StoreOperationResult.SucceededNoId("Payment marked as refunded.");
    }
}
