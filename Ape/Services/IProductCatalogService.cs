using Ape.Models;
using Ape.Models.ViewModels;
using Microsoft.AspNetCore.Http;

namespace Ape.Services;

public interface IProductCatalogService
{
    // Categories
    Task<List<StoreCategoryViewModel>> GetCategoriesAsync(int? parentCategoryId = null, bool activeOnly = true);
    Task<List<StoreCategoryTreeNode>> GetCategoryTreeAsync(int? selectedCategoryId = null);
    Task<StoreCategoryViewModel?> GetCategoryByIdAsync(int categoryId);
    Task<StoreCategoryViewModel?> GetCategoryBySlugAsync(string slug);
    Task<StoreOperationResult> CreateCategoryAsync(CreateStoreCategoryModel model);
    Task<StoreOperationResult> UpdateCategoryAsync(EditStoreCategoryModel model);
    Task<StoreOperationResult> DeleteCategoryAsync(int categoryId);
    Task<StoreOperationResult> UploadCategoryImageAsync(int categoryId, IFormFile file);

    // Products
    Task<List<ProductViewModel>> GetProductsAsync(int? categoryId = null, ProductType? productType = null,
        bool activeOnly = true, bool featuredOnly = false, string? search = null,
        string sortBy = "name", int page = 1, int pageSize = 24);
    Task<int> GetProductCountAsync(int? categoryId = null, ProductType? productType = null,
        bool activeOnly = true, string? search = null);
    Task<ProductViewModel?> GetProductByIdAsync(int productId);
    Task<ProductViewModel?> GetProductBySlugAsync(string slug);
    Task<StoreOperationResult> CreateProductAsync(CreateProductModel model, string createdBy);
    Task<StoreOperationResult> UpdateProductAsync(EditProductModel model);
    Task<StoreOperationResult> DeleteProductAsync(int productId);

    // Product Images
    Task<StoreOperationResult> UploadProductImagesAsync(int productId, IFormFile[] files);
    Task<StoreOperationResult> SetPrimaryImageAsync(int productId, int imageId);
    Task<StoreOperationResult> DeleteProductImageAsync(int imageId);

    // Digital Files
    Task<StoreOperationResult> UploadDigitalFileAsync(int productId, IFormFile file, string uploadedBy);
    Task<StoreOperationResult> DeleteDigitalFileAsync(int fileId);

    // Inventory
    Task<StoreOperationResult> AdjustStockAsync(int productId, int adjustment, string reason);
    Task<List<ProductViewModel>> GetLowStockProductsAsync();

    // Storefront
    Task<StoreHomeViewModel> GetStoreHomeAsync(bool isMember);
    Task<StoreBrowseViewModel> BuildBrowseViewModelAsync(int? categoryId, ProductType? productType,
        string? search, string sortBy, int page, int pageSize, bool isMember);
    Task<ProductDetailViewModel?> GetProductDetailAsync(string slug, bool isMember);
    Task<List<StoreBreadcrumbItem>> GetBreadcrumbsAsync(int? categoryId);

    // Helpers
    string GenerateSlug(string name);
}
