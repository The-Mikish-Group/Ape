using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ape.Models;

public class CustomerDownload
{
    [Key]
    public int DownloadID { get; set; }

    public int OrderItemID { get; set; }

    [Required]
    [MaxLength(450)]
    public required string UserId { get; set; }

    public int ProductID { get; set; }

    public int DigitalFileID { get; set; }

    [Required]
    [MaxLength(100)]
    public required string DownloadToken { get; set; }

    public int DownloadCount { get; set; }

    public int MaxDownloads { get; set; }

    public DateTime? ExpiresDate { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? FirstDownloadDate { get; set; }

    public DateTime? LastDownloadDate { get; set; }

    [ForeignKey("OrderItemID")]
    public virtual OrderItem? OrderItem { get; set; }

    [ForeignKey("ProductID")]
    public virtual Product? Product { get; set; }

    [ForeignKey("DigitalFileID")]
    public virtual DigitalProductFile? DigitalFile { get; set; }
}
