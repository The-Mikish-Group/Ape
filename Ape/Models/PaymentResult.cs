namespace Ape.Models;

public class PaymentResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? TransactionId { get; set; }
    public string? PaymentIntentId { get; set; }
    public string? ClientSecret { get; set; }
    public string? CustomerId { get; set; }
    public string? SubscriptionId { get; set; }
    public string? CardLast4 { get; set; }
    public string? CardBrand { get; set; }
    public string? PayPalEmail { get; set; }
    public string? ApprovalUrl { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }

    public static PaymentResult CreateSuccess(string? transactionId = null) => new()
    {
        Success = true,
        TransactionId = transactionId
    };

    public static PaymentResult CreateFailure(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}
