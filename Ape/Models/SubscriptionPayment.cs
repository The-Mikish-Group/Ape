using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ape.Models;

public class SubscriptionPayment
{
    [Key]
    public int PaymentID { get; set; }

    public int SubscriptionID { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    [MaxLength(20)]
    public required string PaymentGateway { get; set; }

    [MaxLength(200)]
    public required string TransactionId { get; set; }

    [MaxLength(20)]
    public required string Status { get; set; }

    [MaxLength(200)]
    public string? RefundTransactionId { get; set; }

    public DateTime? RefundedDate { get; set; }

    [MaxLength(500)]
    public string? RefundReason { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [ForeignKey("SubscriptionID")]
    public virtual Subscription? Subscription { get; set; }
}
