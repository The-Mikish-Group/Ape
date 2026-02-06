using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ape.Models;

public class OrderItem
{
    [Key]
    public int OrderItemID { get; set; }

    public int OrderID { get; set; }

    public int ProductID { get; set; }

    [Required]
    [MaxLength(300)]
    public required string ProductName { get; set; }

    [MaxLength(100)]
    public string? SKU { get; set; }

    public ProductType ProductType { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal LineTotal { get; set; }

    [ForeignKey("OrderID")]
    public virtual Order? Order { get; set; }

    [ForeignKey("ProductID")]
    public virtual Product? Product { get; set; }
}
