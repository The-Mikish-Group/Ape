namespace Ape.Models.ViewModels;

public class StoreAdminDashboardViewModel
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int LowStockCount { get; set; }
    public decimal TodayRevenue { get; set; }
    public int TodayOrders { get; set; }
    public List<OrderViewModel> RecentOrders { get; set; } = [];
    public List<ProductViewModel> LowStockProducts { get; set; } = [];
}

public class AdminOrderListViewModel
{
    public List<OrderViewModel> Orders { get; set; } = [];
    public OrderStatus? FilterStatus { get; set; }
    public string? SearchQuery { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}

public class AdminOrderDetailViewModel : OrderDetailViewModel
{
    public string? UserId { get; set; }
}

public class SalesReportViewModel
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int PhysicalSales { get; set; }
    public int DigitalSales { get; set; }
    public int SubscriptionSales { get; set; }
    public decimal PhysicalRevenue { get; set; }
    public decimal DigitalRevenue { get; set; }
    public decimal SubscriptionRevenue { get; set; }
    public List<DailySalesData> DailySales { get; set; } = [];
}

public class DailySalesData
{
    public DateTime Date { get; set; }
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
}

public class SubscriptionListViewModel
{
    public int SubscriptionId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public SubscriptionStatus Status { get; set; }
    public string? PaymentGateway { get; set; }
    public decimal Amount { get; set; }
    public string? BillingInterval { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public DateTime CreatedDate { get; set; }

    public string StatusBadgeClass => Status switch
    {
        SubscriptionStatus.Active => "bg-success",
        SubscriptionStatus.PastDue => "bg-warning text-dark",
        SubscriptionStatus.Cancelled => "bg-secondary",
        SubscriptionStatus.Expired => "bg-danger",
        _ => "bg-secondary"
    };
}
