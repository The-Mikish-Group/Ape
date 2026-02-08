using Ape.Models;
using Ape.Models.ViewModels;
using Ape.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ape.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class StoreAdminController(
    IProductCatalogService catalogService,
    IOrderService orderService,
    IStorePaymentService paymentService,
    ISubscriptionService subscriptionService,
    ILogger<StoreAdminController> logger) : Controller
{
    private readonly IProductCatalogService _catalogService = catalogService;
    private readonly IOrderService _orderService = orderService;
    private readonly IStorePaymentService _paymentService = paymentService;
    private readonly ISubscriptionService _subscriptionService = subscriptionService;
    private readonly ILogger<StoreAdminController> _logger = logger;

    // ============================================================
    // Dashboard
    // ============================================================

    public async Task<IActionResult> Index()
    {
        var dashboard = new StoreAdminDashboardViewModel
        {
            TotalRevenue = await _orderService.GetTotalRevenueAsync(),
            TotalOrders = await _orderService.GetOrderCountAsync(),
            PendingOrders = await _orderService.GetOrderCountByStatusAsync(OrderStatus.Pending),
            TodayRevenue = await _orderService.GetTotalRevenueAsync(DateTime.UtcNow.Date),
            TodayOrders = await _orderService.GetOrderCountAsync(search: null),
            ActiveSubscriptions = await _subscriptionService.GetActiveSubscriptionCountAsync(),
            LowStockProducts = await _catalogService.GetLowStockProductsAsync(),
            RecentOrders = await _orderService.GetAllOrdersAsync(pageSize: 5)
        };
        dashboard.LowStockCount = dashboard.LowStockProducts.Count;

        ViewData["Title"] = "Store Admin";
        return View(dashboard);
    }

    // ============================================================
    // Products
    // ============================================================

    public async Task<IActionResult> Products(ProductType? type, bool showInactive = false, string? search = null, int page = 1)
    {
        var products = await _catalogService.GetProductsAsync(
            productType: type,
            activeOnly: !showInactive,
            search: search,
            sortBy: "newest",
            page: page,
            pageSize: 25);

        var totalCount = await _catalogService.GetProductCountAsync(
            productType: type,
            activeOnly: !showInactive,
            search: search);

        ViewData["Title"] = "Manage Products";
        ViewData["FilterType"] = type;
        ViewData["ShowInactive"] = showInactive;
        ViewData["SearchQuery"] = search;
        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = (int)Math.Ceiling((double)totalCount / 25);
        ViewData["TotalCount"] = totalCount;

        return View(products);
    }

    public async Task<IActionResult> CreateProduct()
    {
        ViewData["Title"] = "Create Product";
        ViewData["Categories"] = await _catalogService.GetCategoriesAsync();
        return View(new CreateProductModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(CreateProductModel model, IFormFile[]? images, IFormFile? digitalFile)
    {
        var userEmail = User.Identity?.Name ?? "Unknown";
        var result = await _catalogService.CreateProductAsync(model, userEmail);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            ViewData["Title"] = "Create Product";
            ViewData["Categories"] = await _catalogService.GetCategoriesAsync();
            return View(model);
        }

        // Upload images if provided
        if (images != null && images.Length > 0 && result.EntityId.HasValue)
        {
            await _catalogService.UploadProductImagesAsync(result.EntityId.Value, images);
        }

        // Upload digital file if provided
        if (digitalFile != null && result.EntityId.HasValue)
        {
            await _catalogService.UploadDigitalFileAsync(result.EntityId.Value, digitalFile, userEmail);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(EditProduct), new { id = result.EntityId });
    }

    public async Task<IActionResult> EditProduct(int id)
    {
        var product = await _catalogService.GetProductByIdAsync(id);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Product not found.";
            return RedirectToAction(nameof(Products));
        }

        var model = new EditProductModel
        {
            ProductID = product.ProductId,
            Name = product.Name,
            SKU = product.SKU,
            ProductType = product.ProductType,
            Description = product.Description,
            ShortDescription = product.ShortDescription,
            Price = product.Price,
            CompareAtPrice = product.CompareAtPrice,
            CostPrice = product.CostPrice,
            MemberPrice = product.MemberPrice,
            CategoryID = product.CategoryId,
            IsActive = product.IsActive,
            IsFeatured = product.IsFeatured,
            SortOrder = product.SortOrder,
            StockQuantity = product.StockQuantity,
            LowStockThreshold = product.LowStockThreshold,
            TrackInventory = product.TrackInventory,
            Weight = product.Weight,
            MaxDownloads = product.MaxDownloads,
            DownloadExpiryDays = product.DownloadExpiryDays,
            BillingInterval = product.BillingInterval,
            BillingIntervalCount = product.BillingIntervalCount,
            StripePriceId = product.StripePriceId,
            PayPalPlanId = product.PayPalPlanId
        };

        ViewData["Title"] = $"Edit: {product.Name}";
        ViewData["Categories"] = await _catalogService.GetCategoriesAsync(activeOnly: false);
        ViewData["ProductImages"] = product.Images;
        ViewData["DigitalFiles"] = product.DigitalFiles;
        ViewData["ProductType"] = product.ProductType;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(EditProductModel model)
    {
        var result = await _catalogService.UpdateProductAsync(model);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            ViewData["Title"] = $"Edit: {model.Name}";
            ViewData["Categories"] = await _catalogService.GetCategoriesAsync(activeOnly: false);

            var existingProduct = await _catalogService.GetProductByIdAsync(model.ProductID);
            ViewData["ProductImages"] = existingProduct?.Images ?? [];
            ViewData["DigitalFiles"] = existingProduct?.DigitalFiles ?? [];
            ViewData["ProductType"] = model.ProductType;

            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(EditProduct), new { id = model.ProductID });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var result = await _catalogService.DeleteProductAsync(id);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Products));
    }

    // ============================================================
    // Product Images
    // ============================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadProductImages(int productId, IFormFile[] images)
    {
        var result = await _catalogService.UploadProductImagesAsync(productId, images);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(EditProduct), new { id = productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPrimaryImage(int productId, int imageId)
    {
        var result = await _catalogService.SetPrimaryImageAsync(productId, imageId);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(EditProduct), new { id = productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProductImage(int productId, int imageId)
    {
        var result = await _catalogService.DeleteProductImageAsync(imageId);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(EditProduct), new { id = productId });
    }

    // ============================================================
    // Digital Files
    // ============================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadDigitalFile(int productId, IFormFile digitalFile)
    {
        var userEmail = User.Identity?.Name ?? "Unknown";
        var result = await _catalogService.UploadDigitalFileAsync(productId, digitalFile, userEmail);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(EditProduct), new { id = productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDigitalFile(int productId, int fileId)
    {
        var result = await _catalogService.DeleteDigitalFileAsync(fileId);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(EditProduct), new { id = productId });
    }

    // ============================================================
    // Inventory
    // ============================================================

    public async Task<IActionResult> Inventory()
    {
        var lowStock = await _catalogService.GetLowStockProductsAsync();
        var allPhysical = await _catalogService.GetProductsAsync(productType: ProductType.Physical, activeOnly: true, sortBy: "name", pageSize: 100);

        ViewData["Title"] = "Inventory Management";
        ViewData["LowStockProducts"] = lowStock;

        return View(allPhysical);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustStock(int productId, int adjustment, string? reason)
    {
        var result = await _catalogService.AdjustStockAsync(productId, adjustment, reason ?? "Manual adjustment");
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Inventory));
    }

    // ============================================================
    // Categories
    // ============================================================

    public async Task<IActionResult> Categories()
    {
        var categories = await _catalogService.GetCategoriesAsync(activeOnly: false);
        ViewData["Title"] = "Manage Categories";
        return View(categories);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(CreateStoreCategoryModel model)
    {
        var result = await _catalogService.CreateCategoryAsync(model);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCategory(EditStoreCategoryModel model)
    {
        var result = await _catalogService.UpdateCategoryAsync(model);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int categoryId)
    {
        var result = await _catalogService.DeleteCategoryAsync(categoryId);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadCategoryImage(int categoryId, IFormFile image)
    {
        var result = await _catalogService.UploadCategoryImageAsync(categoryId, image);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCategorySortOrder(int[] categoryIds, int[] sortOrders)
    {
        var result = await _catalogService.UpdateCategorySortOrderAsync(categoryIds, sortOrders);
        return Json(new { success = result.Success, message = result.Message });
    }

    // ============================================================
    // Orders
    // ============================================================

    public async Task<IActionResult> Orders(OrderStatus? status, string? search, int page = 1)
    {
        var orders = await _orderService.GetAllOrdersAsync(status, search, page, 25);
        var totalCount = await _orderService.GetOrderCountAsync(status, search);

        var viewModel = new AdminOrderListViewModel
        {
            Orders = orders,
            FilterStatus = status,
            SearchQuery = search,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling((double)totalCount / 25),
            TotalCount = totalCount
        };

        ViewData["Title"] = "Manage Orders";
        return View(viewModel);
    }

    public async Task<IActionResult> OrderDetail(int id)
    {
        var order = await _orderService.GetOrderDetailAsync(id);
        if (order == null)
        {
            TempData["ErrorMessage"] = "Order not found.";
            return RedirectToAction(nameof(Orders));
        }

        ViewData["Title"] = $"Order {order.OrderNumber}";
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(int orderId, OrderStatus status)
    {
        var result = await _orderService.UpdateOrderStatusAsync(orderId, status);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(OrderDetail), new { id = orderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTracking(int orderId, string carrier, string trackingNumber)
    {
        var result = await _orderService.AddTrackingAsync(orderId, carrier, trackingNumber);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(OrderDetail), new { id = orderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAdminNotes(int orderId, string notes)
    {
        var result = await _orderService.UpdateAdminNotesAsync(orderId, notes);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(OrderDetail), new { id = orderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RefundOrder(int orderId, string? reason)
    {
        var order = await _orderService.GetOrderDetailAsync(orderId);
        if (order == null || !order.IsPaid)
        {
            TempData["ErrorMessage"] = "Order not found or not paid.";
            return RedirectToAction(nameof(OrderDetail), new { id = orderId });
        }

        var result = order.PaymentGateway switch
        {
            "Stripe" => await _paymentService.RefundStripePaymentAsync(order.PaymentTransactionId ?? ""),
            "PayPal" => await _paymentService.RefundPayPalPaymentAsync(order.PaymentTransactionId ?? ""),
            _ => PaymentResult.CreateFailure($"Unknown payment gateway: {order.PaymentGateway}")
        };

        if (result.Success)
        {
            await _orderService.UpdateOrderStatusAsync(orderId, OrderStatus.Refunded);
            TempData["SuccessMessage"] = "Order refunded successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = $"Refund failed: {result.ErrorMessage}";
        }

        return RedirectToAction(nameof(OrderDetail), new { id = orderId });
    }

    // ============================================================
    // Sales Report
    // ============================================================

    public async Task<IActionResult> SalesReport(DateTime? startDate, DateTime? endDate)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30).Date;
        var end = endDate ?? DateTime.UtcNow.Date;

        var report = await _orderService.GetSalesReportAsync(start, end);

        ViewData["Title"] = "Sales Report";
        return View(report);
    }

    // ============================================================
    // Subscriptions
    // ============================================================

    public async Task<IActionResult> Subscriptions(SubscriptionStatus? status, int page = 1)
    {
        var subscriptions = await _subscriptionService.GetAllSubscriptionsAsync(status, page);
        var totalCount = await _subscriptionService.GetSubscriptionCountAsync(status);

        ViewData["Title"] = "Manage Subscriptions";
        ViewData["FilterStatus"] = status;
        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = (int)Math.Ceiling((double)totalCount / 25);
        ViewData["TotalCount"] = totalCount;

        return View(subscriptions);
    }

    public async Task<IActionResult> SubscriptionDetail(int id)
    {
        var detail = await _subscriptionService.GetSubscriptionDetailByIdAsync(id);
        if (detail == null)
        {
            TempData["ErrorMessage"] = "Subscription not found.";
            return RedirectToAction(nameof(Subscriptions));
        }

        ViewData["Title"] = $"Subscription: {detail.ProductName}";
        return View(detail);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RefundSubscriptionPayment(int paymentId, string? reason)
    {
        var payment = await _subscriptionService.GetPaymentByIdAsync(paymentId);
        if (payment == null || payment.Status != "Paid")
        {
            TempData["ErrorMessage"] = "Payment not found or not eligible for refund.";
            return RedirectToAction(nameof(Subscriptions));
        }

        var refundResult = payment.PaymentGateway switch
        {
            "Stripe" => await _paymentService.RefundStripePaymentAsync(payment.TransactionId),
            "PayPal" => await _paymentService.RefundPayPalPaymentAsync(payment.TransactionId),
            _ => PaymentResult.CreateFailure($"Unknown payment gateway: {payment.PaymentGateway}")
        };

        if (refundResult.Success)
        {
            await _subscriptionService.MarkPaymentRefundedAsync(
                paymentId, refundResult.TransactionId ?? "unknown", reason);
            TempData["SuccessMessage"] = "Payment refunded successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = $"Refund failed: {refundResult.ErrorMessage}";
        }

        return RedirectToAction(nameof(SubscriptionDetail), new { id = payment.SubscriptionID });
    }
}
