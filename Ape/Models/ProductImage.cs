using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ape.Models;

public class ProductImage
{
    [Key]
    public int ImageID { get; set; }

    public int ProductID { get; set; }

    [Required]
    [MaxLength(500)]
    public required string FileName { get; set; }

    [MaxLength(500)]
    public string? OriginalFileName { get; set; }

    [MaxLength(300)]
    public string? AltText { get; set; }

    public int SortOrder { get; set; }

    public bool IsPrimary { get; set; }

    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

    [ForeignKey("ProductID")]
    public virtual Product? Product { get; set; }

    [NotMapped]
    public string ThumbnailFileName
    {
        get
        {
            if (string.IsNullOrEmpty(FileName)) return string.Empty;
            var ext = Path.GetExtension(FileName);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(FileName);
            return $"{nameWithoutExt}_thumb{ext}";
        }
    }

    [NotMapped]
    public string ImageUrl => $"/store/products/{FileName}";

    [NotMapped]
    public string ThumbnailUrl => $"/store/products/{ThumbnailFileName}";
}
