using System.ComponentModel.DataAnnotations;

namespace Ape.Models;

public class ShippingAddress
{
    [Key]
    public int AddressID { get; set; }

    [Required]
    [MaxLength(450)]
    public required string UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public required string FullName { get; set; }

    [Required]
    [MaxLength(200)]
    public required string AddressLine1 { get; set; }

    [MaxLength(200)]
    public string? AddressLine2 { get; set; }

    [Required]
    [MaxLength(100)]
    public required string City { get; set; }

    [MaxLength(50)]
    public string? State { get; set; }

    [Required]
    [MaxLength(20)]
    public required string ZipCode { get; set; }

    [MaxLength(50)]
    public string Country { get; set; } = "US";

    [MaxLength(20)]
    public string? Phone { get; set; }

    public bool IsDefault { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
