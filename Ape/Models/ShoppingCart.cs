using System.ComponentModel.DataAnnotations;

namespace Ape.Models;

public class ShoppingCart
{
    [Key]
    public int CartID { get; set; }

    [Required]
    [MaxLength(450)]
    public required string UserId { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    public virtual ICollection<ShoppingCartItem> Items { get; set; } = [];
}
