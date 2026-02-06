namespace Ape.Models.ViewModels;

public class OrderViewModel
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public bool HasPhysicalItems { get; set; }
    public bool HasDigitalItems { get; set; }
    public bool IsPaid { get; set; }
    public string? PaymentGateway { get; set; }
    public string? TrackingNumber { get; set; }
    public string? ShippingCarrier { get; set; }

    public string StatusBadgeClass => Status switch
    {
        OrderStatus.Pending => "store-status-pending",
        OrderStatus.Processing => "store-status-processing",
        OrderStatus.Shipped => "store-status-shipped",
        OrderStatus.Delivered => "store-status-delivered",
        OrderStatus.Completed => "store-status-completed",
        OrderStatus.Cancelled => "store-status-cancelled",
        OrderStatus.Refunded => "store-status-refunded",
        _ => "bg-secondary"
    };
}

public class OrderDetailViewModel
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public bool HasPhysicalItems { get; set; }
    public bool HasDigitalItems { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidDate { get; set; }
    public string? PaymentGateway { get; set; }
    public string? PaymentTransactionId { get; set; }
    public string? CustomerNotes { get; set; }
    public string? AdminNotes { get; set; }
    public string? CustomerEmail { get; set; }

    // Shipping
    public string? ShipToName { get; set; }
    public string? ShipToAddress { get; set; }
    public string? ShippingMethod { get; set; }
    public string? TrackingNumber { get; set; }
    public string? ShippingCarrier { get; set; }
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }

    // Refund
    public string? RefundTransactionId { get; set; }
    public decimal? RefundedAmount { get; set; }
    public DateTime? RefundedDate { get; set; }
    public string? RefundReason { get; set; }

    public List<OrderItemViewModel> Items { get; set; } = [];
    public List<DownloadLinkViewModel> Downloads { get; set; } = [];

    public string StatusBadgeClass => Status switch
    {
        OrderStatus.Pending => "store-status-pending",
        OrderStatus.Processing => "store-status-processing",
        OrderStatus.Shipped => "store-status-shipped",
        OrderStatus.Delivered => "store-status-delivered",
        OrderStatus.Completed => "store-status-completed",
        OrderStatus.Cancelled => "store-status-cancelled",
        OrderStatus.Refunded => "store-status-refunded",
        _ => "bg-secondary"
    };
}

public class OrderItemViewModel
{
    public int OrderItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public ProductType ProductType { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
