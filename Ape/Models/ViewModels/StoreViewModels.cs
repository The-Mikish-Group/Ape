namespace Ape.Models.ViewModels;

// ============================================================
// Category ViewModels
// ============================================================

public class StoreCategoryViewModel
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentCategoryId { get; set; }
    public string? ParentCategoryName { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public string? ImageFileName { get; set; }
    public string? ImageUrl => ImageFileName != null ? $"/store/categories/{ImageFileName}" : null;
    public int ProductCount { get; set; }
    public bool HasChildren { get; set; }
}

public class StoreCategoryTreeNode
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
    public bool IsExpanded { get; set; }
    public bool IsSelected { get; set; }
    public int ProductCount { get; set; }
    public List<StoreCategoryTreeNode> Children { get; set; } = [];
}

public class CreateStoreCategoryModel
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentCategoryId { get; set; }
    public int SortOrder { get; set; }
}

public class EditStoreCategoryModel
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentCategoryId { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

// ============================================================
// Product ViewModels
// ============================================================

public class ProductViewModel
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public ProductType ProductType { get; set; }
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public decimal? CostPrice { get; set; }
    public decimal? MemberPrice { get; set; }
    public decimal DisplayPrice { get; set; }
    public bool ShowMemberPrice { get; set; }
    public bool IsOnSale => CompareAtPrice.HasValue && CompareAtPrice > Price;
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? CategorySlug { get; set; }

    // Physical
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; }
    public bool TrackInventory { get; set; }
    public decimal? Weight { get; set; }
    public bool IsInStock => !TrackInventory || StockQuantity > 0;
    public bool IsLowStock => TrackInventory && StockQuantity > 0 && StockQuantity <= LowStockThreshold;

    // Digital
    public int MaxDownloads { get; set; }
    public int DownloadExpiryDays { get; set; }

    // Subscription
    public string? BillingInterval { get; set; }
    public int BillingIntervalCount { get; set; }
    public string? StripePriceId { get; set; }
    public string? PayPalPlanId { get; set; }

    public bool IsActive { get; set; }
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }

    public string? PrimaryImageUrl { get; set; }
    public string? PrimaryThumbnailUrl { get; set; }
    public List<ProductImageViewModel> Images { get; set; } = [];
    public List<DigitalFileViewModel> DigitalFiles { get; set; } = [];

    public string ProductTypeBadgeClass => ProductType switch
    {
        ProductType.Physical => "bg-primary",
        ProductType.Digital => "bg-info",
        ProductType.Subscription => "bg-success",
        _ => "bg-secondary"
    };
}

public class ProductImageViewModel
{
    public int ImageId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public string? AltText { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
    public string ImageUrl => $"/store/products/{FileName}";
    public string ThumbnailUrl
    {
        get
        {
            if (string.IsNullOrEmpty(FileName)) return string.Empty;
            var ext = Path.GetExtension(FileName);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(FileName);
            return $"/store/products/{nameWithoutExt}_thumb{ext}";
        }
    }
}

public class DigitalFileViewModel
{
    public int FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public long FileSize { get; set; }
    public string? ContentType { get; set; }
    public DateTime UploadedDate { get; set; }
    public string FormattedFileSize => FileSize switch
    {
        < 1024 => $"{FileSize} B",
        < 1024 * 1024 => $"{FileSize / 1024.0:F1} KB",
        _ => $"{FileSize / (1024.0 * 1024.0):F1} MB"
    };
}

public class CreateProductModel
{
    public string Name { get; set; } = string.Empty;
    public ProductType ProductType { get; set; }
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public decimal? CostPrice { get; set; }
    public decimal? MemberPrice { get; set; }
    public int? CategoryID { get; set; }

    // Physical
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 5;
    public bool TrackInventory { get; set; }
    public decimal? Weight { get; set; }

    // Digital
    public int MaxDownloads { get; set; } = 5;
    public int DownloadExpiryDays { get; set; } = 30;

    // Subscription
    public string? BillingInterval { get; set; }
    public int BillingIntervalCount { get; set; } = 1;
    public string? StripePriceId { get; set; }
    public string? PayPalPlanId { get; set; }
}

public class EditProductModel
{
    public int ProductID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public ProductType ProductType { get; set; }
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public decimal? CostPrice { get; set; }
    public decimal? MemberPrice { get; set; }
    public int? CategoryID { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }

    // Physical
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 5;
    public bool TrackInventory { get; set; }
    public decimal? Weight { get; set; }

    // Digital
    public int MaxDownloads { get; set; } = 5;
    public int DownloadExpiryDays { get; set; } = 30;

    // Subscription
    public string? BillingInterval { get; set; }
    public int BillingIntervalCount { get; set; } = 1;
    public string? StripePriceId { get; set; }
    public string? PayPalPlanId { get; set; }
}

// ============================================================
// Browse / Storefront ViewModels
// ============================================================

public class StoreBrowseViewModel
{
    public int? CurrentCategoryId { get; set; }
    public string CurrentCategoryName { get; set; } = "Shop";
    public string? CurrentCategorySlug { get; set; }
    public string? CurrentCategoryDescription { get; set; }
    public List<StoreBreadcrumbItem> Breadcrumbs { get; set; } = [];
    public List<StoreCategoryViewModel> Categories { get; set; } = [];
    public List<ProductViewModel> Products { get; set; } = [];
    public List<StoreCategoryTreeNode> CategoryTree { get; set; } = [];
    public ProductType? FilterProductType { get; set; }
    public string? SearchQuery { get; set; }
    public string SortBy { get; set; } = "name";

    // Pagination
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 24;
    public int TotalPages { get; set; }
    public int TotalProducts { get; set; }
}

public class StoreBreadcrumbItem
{
    public int? CategoryId { get; set; }
    public string? Slug { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
}

public class StoreHomeViewModel
{
    public List<ProductViewModel> FeaturedProducts { get; set; } = [];
    public List<StoreCategoryViewModel> Categories { get; set; } = [];
    public List<ProductViewModel> SubscriptionProducts { get; set; } = [];
    public string StoreName { get; set; } = "Shop";
}

public class ProductDetailViewModel
{
    public ProductViewModel Product { get; set; } = new();
    public List<ProductViewModel> RelatedProducts { get; set; } = [];
    public List<StoreBreadcrumbItem> Breadcrumbs { get; set; } = [];
}

// ============================================================
// Operation Results
// ============================================================

// ============================================================
// Subscription ViewModels
// ============================================================

public class SubscriptionDetailViewModel
{
    public int SubscriptionId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductDescription { get; set; }
    public SubscriptionStatus Status { get; set; }
    public string? PaymentGateway { get; set; }
    public decimal Amount { get; set; }
    public string? BillingInterval { get; set; }
    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public DateTime? CancelledDate { get; set; }
    public string? CancelReason { get; set; }
    public DateTime CreatedDate { get; set; }

    public string StatusBadgeClass => Status switch
    {
        SubscriptionStatus.Active => "bg-success",
        SubscriptionStatus.PastDue => "bg-warning text-dark",
        SubscriptionStatus.Cancelled => "bg-secondary",
        SubscriptionStatus.Expired => "bg-danger",
        _ => "bg-secondary"
    };

    public string BillingDisplay => BillingInterval switch
    {
        "month" => $"{Amount:C}/month",
        "year" => $"{Amount:C}/year",
        _ => $"{Amount:C}/{BillingInterval}"
    };
}

public class SubscribeViewModel
{
    public ProductViewModel Product { get; set; } = new();
    public bool StripeEnabled { get; set; }
    public bool PayPalEnabled { get; set; }
    public string? StripePublishableKey { get; set; }
    public string? PayPalClientId { get; set; }
    public string? StripeClientSecret { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? PayPalPlanId { get; set; }
    public string? ErrorMessage { get; set; }
}

public class StoreOperationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int? EntityId { get; set; }

    public static StoreOperationResult Succeeded(int entityId, string? message = null)
        => new() { Success = true, EntityId = entityId, Message = message };

    public static StoreOperationResult SucceededNoId(string? message = null)
        => new() { Success = true, Message = message };

    public static StoreOperationResult Failed(string message)
        => new() { Success = false, Message = message };
}
