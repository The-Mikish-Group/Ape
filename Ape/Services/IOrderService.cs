using Ape.Models;
using Ape.Models.ViewModels;

namespace Ape.Services;

public interface IOrderService
{
    Task<(StoreOperationResult Result, int? OrderId)> CreateOrderFromCartAsync(string userId, int? shippingAddressId, string? customerNotes);
    Task<OrderDetailViewModel?> GetOrderDetailAsync(int orderId);
    Task<OrderDetailViewModel?> GetOrderDetailByNumberAsync(string orderNumber);
    Task<List<OrderViewModel>> GetUserOrdersAsync(string userId, int page = 1, int pageSize = 20);
    Task<List<OrderViewModel>> GetAllOrdersAsync(OrderStatus? status = null, string? search = null, int page = 1, int pageSize = 25);
    Task<int> GetOrderCountAsync(OrderStatus? status = null, string? search = null);
    Task<StoreOperationResult> UpdateOrderStatusAsync(int orderId, OrderStatus status);
    Task<StoreOperationResult> AddTrackingAsync(int orderId, string carrier, string trackingNumber);
    Task<StoreOperationResult> MarkOrderPaidAsync(int orderId, string gateway, string transactionId, string? paymentIntentId = null);
    Task<StoreOperationResult> UpdateAdminNotesAsync(int orderId, string notes);
    Task<string> GenerateOrderNumberAsync();

    // Dashboard stats
    Task<decimal> GetTotalRevenueAsync(DateTime? since = null);
    Task<int> GetOrderCountByStatusAsync(OrderStatus status);
    Task<SalesReportViewModel> GetSalesReportAsync(DateTime startDate, DateTime endDate);
}
