using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ape.Models;

public class Product
{
    [Key]
    public int ProductID { get; set; }

    [Required]
    [MaxLength(300)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(300)]
    public required string Slug { get; set; }

    [Required]
    [MaxLength(100)]
    public required string SKU { get; set; }

    public ProductType ProductType { get; set; }

    [MaxLength(5000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? ShortDescription { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? CompareAtPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? CostPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MemberPrice { get; set; }

    public int? CategoryID { get; set; }

    // Physical product fields
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 5;
    public bool TrackInventory { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Weight { get; set; }

    // Subscription fields
    [MaxLength(50)]
    public string? BillingInterval { get; set; }

    public int BillingIntervalCount { get; set; } = 1;

    [MaxLength(100)]
    public string? StripePriceId { get; set; }

    [MaxLength(100)]
    public string? PayPalPlanId { get; set; }

    // Digital product fields
    public int MaxDownloads { get; set; } = 5;
    public int DownloadExpiryDays { get; set; } = 30;

    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    [MaxLength(256)]
    public string? CreatedBy { get; set; }

    [ForeignKey("CategoryID")]
    public virtual StoreCategory? Category { get; set; }

    public virtual ICollection<ProductImage> Images { get; set; } = [];
    public virtual ICollection<DigitalProductFile> DigitalFiles { get; set; } = [];
}
