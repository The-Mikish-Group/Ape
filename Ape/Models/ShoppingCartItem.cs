using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ape.Models;

public class ShoppingCartItem
{
    [Key]
    public int CartItemID { get; set; }

    public int CartID { get; set; }

    public int ProductID { get; set; }

    public int Quantity { get; set; } = 1;

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    public DateTime AddedDate { get; set; } = DateTime.UtcNow;

    [ForeignKey("CartID")]
    public virtual ShoppingCart? Cart { get; set; }

    [ForeignKey("ProductID")]
    public virtual Product? Product { get; set; }
}
