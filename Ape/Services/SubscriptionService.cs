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
}
