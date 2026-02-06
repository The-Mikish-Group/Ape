using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ape.Models;

public class StoreCategory
{
    [Key]
    public int CategoryID { get; set; }

    [Required]
    [MaxLength(200)]
    public required string CategoryName { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Slug { get; set; }

    public int? ParentCategoryID { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? ImageFileName { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    [ForeignKey("ParentCategoryID")]
    public virtual StoreCategory? ParentCategory { get; set; }

    public virtual ICollection<StoreCategory> ChildCategories { get; set; } = [];

    public virtual ICollection<Product> Products { get; set; } = [];
}
