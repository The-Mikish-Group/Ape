using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ape.Models;

public class Subscription
{
    [Key]
    public int SubscriptionID { get; set; }

    [Required]
    [MaxLength(450)]
    public required string UserId { get; set; }

    public int ProductID { get; set; }

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    [MaxLength(20)]
    public string? PaymentGateway { get; set; }

    [MaxLength(100)]
    public string? StripeSubscriptionId { get; set; }

    [MaxLength(100)]
    public string? PayPalSubscriptionId { get; set; }

    public DateTime? CurrentPeriodStart { get; set; }

    public DateTime? CurrentPeriodEnd { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [MaxLength(50)]
    public string? BillingInterval { get; set; }

    public DateTime? CancelledDate { get; set; }

    [MaxLength(500)]
    public string? CancelReason { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    [ForeignKey("ProductID")]
    public virtual Product? Product { get; set; }
}
