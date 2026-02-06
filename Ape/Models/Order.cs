using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ape.Models;

public class Order
{
    [Key]
    public int OrderID { get; set; }

    [Required]
    [MaxLength(50)]
    public required string OrderNumber { get; set; }

    [Required]
    [MaxLength(450)]
    public required string UserId { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public bool HasPhysicalItems { get; set; }
    public bool HasDigitalItems { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ShippingCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    // Shipping address snapshot
    [MaxLength(100)]
    public string? ShipToName { get; set; }

    [MaxLength(200)]
    public string? ShipToAddress1 { get; set; }

    [MaxLength(200)]
    public string? ShipToAddress2 { get; set; }

    [MaxLength(100)]
    public string? ShipToCity { get; set; }

    [MaxLength(50)]
    public string? ShipToState { get; set; }

    [MaxLength(20)]
    public string? ShipToZip { get; set; }

    [MaxLength(50)]
    public string? ShipToCountry { get; set; }

    [MaxLength(20)]
    public string? ShipToPhone { get; set; }

    // Shipping tracking
    [MaxLength(100)]
    public string? ShippingMethod { get; set; }

    [MaxLength(200)]
    public string? TrackingNumber { get; set; }

    [MaxLength(100)]
    public string? ShippingCarrier { get; set; }

    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }

    // Payment
    [MaxLength(20)]
    public string? PaymentGateway { get; set; }

    [MaxLength(200)]
    public string? PaymentTransactionId { get; set; }

    [MaxLength(200)]
    public string? PaymentIntentId { get; set; }

    public bool IsPaid { get; set; }
    public DateTime? PaidDate { get; set; }

    // Refund
    [MaxLength(200)]
    public string? RefundTransactionId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? RefundedAmount { get; set; }

    public DateTime? RefundedDate { get; set; }

    [MaxLength(500)]
    public string? RefundReason { get; set; }

    // Notes
    [MaxLength(1000)]
    public string? CustomerNotes { get; set; }

    [MaxLength(1000)]
    public string? AdminNotes { get; set; }

    [MaxLength(256)]
    public string? CustomerEmail { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    public virtual ICollection<OrderItem> Items { get; set; } = [];
}
