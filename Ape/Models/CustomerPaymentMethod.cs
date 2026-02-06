using System.ComponentModel.DataAnnotations;

namespace Ape.Models;

public class CustomerPaymentMethod
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public required string UserId { get; set; }

    // Stripe
    [MaxLength(100)]
    public string? StripeCustomerId { get; set; }

    [MaxLength(100)]
    public string? StripePaymentMethodId { get; set; }

    [MaxLength(4)]
    public string? CardLast4 { get; set; }

    [MaxLength(20)]
    public string? CardBrand { get; set; }

    // PayPal
    [MaxLength(100)]
    public string? PayPalPayerId { get; set; }

    [MaxLength(256)]
    public string? PayPalEmail { get; set; }

    [MaxLength(20)]
    public string PreferredGateway { get; set; } = "Stripe";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
