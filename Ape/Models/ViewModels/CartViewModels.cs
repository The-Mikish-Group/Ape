namespace Ape.Models.ViewModels;

public class CartViewModel
{
    public int CartId { get; set; }
    public List<CartItemViewModel> Items { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Total { get; set; }
    public bool HasPhysicalItems { get; set; }
    public bool HasDigitalItems { get; set; }
    public bool HasOutOfStockItems { get; set; }
    public int ItemCount { get; set; }
    public string? FreeShippingMessage { get; set; }
}

public class CartItemViewModel
{
    public int CartItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public ProductType ProductType { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public bool IsInStock { get; set; } = true;
    public int AvailableStock { get; set; }
    public bool TrackInventory { get; set; }
}

public class AddToCartRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class UpdateCartQuantityRequest
{
    public int CartItemId { get; set; }
    public int Quantity { get; set; }
}

public class RemoveFromCartRequest
{
    public int CartItemId { get; set; }
}

public class ShippingAddressViewModel
{
    public int AddressId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? State { get; set; }
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = "US";
    public string? Phone { get; set; }
    public bool IsDefault { get; set; }

    public string FormattedAddress => string.IsNullOrEmpty(AddressLine2)
        ? $"{AddressLine1}, {City}, {State} {ZipCode}"
        : $"{AddressLine1}, {AddressLine2}, {City}, {State} {ZipCode}";
}

public class CreateShippingAddressModel
{
    public string FullName { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? State { get; set; }
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = "US";
    public string? Phone { get; set; }
    public bool IsDefault { get; set; }
}

public class EditShippingAddressModel
{
    public int AddressId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? State { get; set; }
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = "US";
    public string? Phone { get; set; }
    public bool IsDefault { get; set; }
}
