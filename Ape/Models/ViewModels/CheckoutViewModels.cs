namespace Ape.Models.ViewModels;

public class CheckoutViewModel
{
    public CartViewModel Cart { get; set; } = new();
    public List<ShippingAddressViewModel> Addresses { get; set; } = [];
    public int? SelectedAddressId { get; set; }
    public bool RequiresShipping { get; set; }
}

public class PaymentViewModel
{
    public string OrderNumber { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public decimal TotalAmount { get; set; }
    public string? StripePublishableKey { get; set; }
    public string? PayPalClientId { get; set; }
    public string? ClientSecret { get; set; }
    public bool StripeEnabled { get; set; }
    public bool PayPalEnabled { get; set; }
}

public class OrderConfirmationViewModel
{
    public string OrderNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string PaymentGateway { get; set; } = string.Empty;
    public bool HasDigitalItems { get; set; }
    public bool HasPhysicalItems { get; set; }
    public List<OrderItemViewModel> Items { get; set; } = [];
    public List<DownloadLinkViewModel> Downloads { get; set; } = [];
}

public class DownloadLinkViewModel
{
    public string ProductName { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public string DownloadToken { get; set; } = string.Empty;
    public int DownloadCount { get; set; }
    public int MaxDownloads { get; set; }
    public DateTime? ExpiresDate { get; set; }
    public bool IsExpired => ExpiresDate.HasValue && ExpiresDate < DateTime.UtcNow;
    public bool IsMaxedOut => DownloadCount >= MaxDownloads;
    public bool CanDownload => !IsExpired && !IsMaxedOut;
}
