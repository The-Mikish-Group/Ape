using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ape.Models;

public class DigitalProductFile
{
    [Key]
    public int FileID { get; set; }

    public int ProductID { get; set; }

    [Required]
    [MaxLength(500)]
    public required string FileName { get; set; }

    [MaxLength(500)]
    public string? OriginalFileName { get; set; }

    public long FileSize { get; set; }

    [MaxLength(100)]
    public string? ContentType { get; set; }

    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

    [MaxLength(256)]
    public string? UploadedBy { get; set; }

    [ForeignKey("ProductID")]
    public virtual Product? Product { get; set; }
}
