using System.Text.RegularExpressions;
using Ape.Data;
using Ape.Models;
using Ape.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Ape.Services;

public partial class ProductCatalogService(
    ApplicationDbContext context,
    IWebHostEnvironment environment,
    IImageOptimizationService imageOptimization,
    ILogger<ProductCatalogService> logger) : IProductCatalogService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IWebHostEnvironment _environment = environment;
    private readonly IImageOptimizationService _imageOptimization = imageOptimization;
    private readonly ILogger<ProductCatalogService> _logger = logger;
    private readonly string _productImagesPath = Path.Combine(environment.WebRootPath, "store", "products");
    private readonly string _categoryImagesPath = Path.Combine(environment.WebRootPath, "store", "categories");
    private readonly string _digitalFilesPath = Path.Combine(environment.ContentRootPath, "ProtectedFiles", "store");

    // ============================================================
    // Categories
    // ============================================================

    public async Task<List<StoreCategoryViewModel>> GetCategoriesAsync(int? parentCategoryId = null, bool activeOnly = true)
    {
        var query = _context.StoreCategories.AsNoTracking().AsQueryable();

        if (activeOnly)
            query = query.Where(c => c.IsActive);

        query = query.Where(c => c.ParentCategoryID == parentCategoryId);

        return await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.CategoryName)
            .Select(c => new StoreCategoryViewModel
            {
                CategoryId = c.CategoryID,
                Name = c.CategoryName,
                Slug = c.Slug,
                Description = c.Description,
                ParentCategoryId = c.ParentCategoryID,
                SortOrder = c.SortOrder,
                IsActive = c.IsActive,
                ImageFileName = c.ImageFileName,
                ProductCount = c.Products.Count(p => p.IsActive),
                HasChildren = c.ChildCategories.Any(cc => cc.IsActive)
            })
            .ToListAsync();
    }

    public async Task<List<StoreCategoryTreeNode>> GetCategoryTreeAsync(int? selectedCategoryId = null)
    {
        var allCategories = await _context.StoreCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.CategoryName)
            .Select(c => new StoreCategoryTreeNode
            {
                CategoryId = c.CategoryID,
                Name = c.CategoryName,
                Slug = c.Slug,
                ParentCategoryId = c.ParentCategoryID,
                ProductCount = c.Products.Count(p => p.IsActive)
            })
            .ToListAsync();

        var lookup = allCategories.ToLookup(c => c.ParentCategoryId);
        var roots = lookup[null].ToList();

        void BuildTree(List<StoreCategoryTreeNode> nodes)
        {
            foreach (var node in nodes)
            {
                node.Children = lookup[node.CategoryId].ToList();
                if (node.CategoryId == selectedCategoryId)
                    node.IsSelected = true;
                BuildTree(node.Children);
            }
        }

        BuildTree(roots);
        return roots;
    }

    public async Task<StoreCategoryViewModel?> GetCategoryByIdAsync(int categoryId)
    {
        return await _context.StoreCategories
            .AsNoTracking()
            .Where(c => c.CategoryID == categoryId)
            .Select(c => new StoreCategoryViewModel
            {
                CategoryId = c.CategoryID,
                Name = c.CategoryName,
                Slug = c.Slug,
                Description = c.Description,
                ParentCategoryId = c.ParentCategoryID,
                ParentCategoryName = c.ParentCategory != null ? c.ParentCategory.CategoryName : null,
                SortOrder = c.SortOrder,
                IsActive = c.IsActive,
                ImageFileName = c.ImageFileName,
                ProductCount = c.Products.Count(p => p.IsActive),
                HasChildren = c.ChildCategories.Any()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<StoreCategoryViewModel?> GetCategoryBySlugAsync(string slug)
    {
        return await _context.StoreCategories
            .AsNoTracking()
            .Where(c => c.Slug == slug && c.IsActive)
            .Select(c => new StoreCategoryViewModel
            {
                CategoryId = c.CategoryID,
                Name = c.CategoryName,
                Slug = c.Slug,
                Description = c.Description,
                ParentCategoryId = c.ParentCategoryID,
                ParentCategoryName = c.ParentCategory != null ? c.ParentCategory.CategoryName : null,
                SortOrder = c.SortOrder,
                IsActive = c.IsActive,
                ImageFileName = c.ImageFileName,
                ProductCount = c.Products.Count(p => p.IsActive),
                HasChildren = c.ChildCategories.Any(cc => cc.IsActive)
            })
            .FirstOrDefaultAsync();
    }

    public async Task<StoreOperationResult> CreateCategoryAsync(CreateStoreCategoryModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            return StoreOperationResult.Failed("Category name is required.");

        var slug = GenerateSlug(model.Name);
        var existingSlug = await _context.StoreCategories.AnyAsync(c => c.Slug == slug && c.IsActive);
        if (existingSlug)
            slug = $"{slug}-{DateTime.UtcNow.Ticks % 10000}";

        var category = new StoreCategory
        {
            CategoryName = model.Name.Trim(),
            Slug = slug,
            Description = model.Description?.Trim(),
            ParentCategoryID = model.ParentCategoryId,
            SortOrder = model.SortOrder,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        _context.StoreCategories.Add(category);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created store category '{CategoryName}' (ID: {CategoryId})", category.CategoryName, category.CategoryID);
        return StoreOperationResult.Succeeded(category.CategoryID, $"Category '{category.CategoryName}' created.");
    }

    public async Task<StoreOperationResult> UpdateCategoryAsync(EditStoreCategoryModel model)
    {
        var category = await _context.StoreCategories.FindAsync(model.CategoryId);
        if (category == null)
            return StoreOperationResult.Failed("Category not found.");

        if (string.IsNullOrWhiteSpace(model.Name))
            return StoreOperationResult.Failed("Category name is required.");

        if (category.CategoryName != model.Name.Trim())
        {
            var slug = GenerateSlug(model.Name);
            var existingSlug = await _context.StoreCategories.AnyAsync(c => c.Slug == slug && c.IsActive && c.CategoryID != model.CategoryId);
            if (existingSlug)
                slug = $"{slug}-{DateTime.UtcNow.Ticks % 10000}";
            category.Slug = slug;
        }

        category.CategoryName = model.Name.Trim();
        category.Description = model.Description?.Trim();
        category.ParentCategoryID = model.ParentCategoryId;
        category.SortOrder = model.SortOrder;
        category.IsActive = model.IsActive;
        category.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated store category '{CategoryName}' (ID: {CategoryId})", category.CategoryName, category.CategoryID);
        return StoreOperationResult.Succeeded(category.CategoryID, $"Category '{category.CategoryName}' updated.");
    }

    public async Task<StoreOperationResult> DeleteCategoryAsync(int categoryId)
    {
        var category = await _context.StoreCategories
            .Include(c => c.Products)
            .Include(c => c.ChildCategories)
            .FirstOrDefaultAsync(c => c.CategoryID == categoryId);

        if (category == null)
            return StoreOperationResult.Failed("Category not found.");

        if (category.Products.Any(p => p.IsActive))
            return StoreOperationResult.Failed("Cannot delete a category that has active products. Move or deactivate products first.");

        if (category.ChildCategories.Any(c => c.IsActive))
            return StoreOperationResult.Failed("Cannot delete a category that has active subcategories.");

        category.IsActive = false;
        category.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Soft-deleted store category '{CategoryName}' (ID: {CategoryId})", category.CategoryName, category.CategoryID);
        return StoreOperationResult.SucceededNoId($"Category '{category.CategoryName}' deleted.");
    }

    public async Task<StoreOperationResult> UploadCategoryImageAsync(int categoryId, IFormFile file)
    {
        var category = await _context.StoreCategories.FindAsync(categoryId);
        if (category == null)
            return StoreOperationResult.Failed("Category not found.");

        if (!_imageOptimization.IsValidImageFormat(file))
            return StoreOperationResult.Failed("Invalid image format.");

        Directory.CreateDirectory(_categoryImagesPath);

        var fileName = $"cat_{categoryId}_{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";

        using var stream = file.OpenReadStream();
        var optimized = await _imageOptimization.OptimizeImageAsync(stream, 800);
        await File.WriteAllBytesAsync(Path.Combine(_categoryImagesPath, fileName), optimized);

        // Delete old image
        if (!string.IsNullOrEmpty(category.ImageFileName))
        {
            var oldPath = Path.Combine(_categoryImagesPath, category.ImageFileName);
            if (File.Exists(oldPath)) File.Delete(oldPath);
        }

        category.ImageFileName = fileName;
        category.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return StoreOperationResult.Succeeded(categoryId, "Category image uploaded.");
    }

    public async Task<StoreOperationResult> UpdateCategorySortOrderAsync(int[] categoryIds, int[] sortOrders)
    {
        if (categoryIds.Length != sortOrders.Length)
            return StoreOperationResult.Failed("Invalid sort order data.");

        for (int i = 0; i < categoryIds.Length; i++)
        {
            var category = await _context.StoreCategories.FindAsync(categoryIds[i]);
            if (category != null)
                category.SortOrder = sortOrders[i];
        }

        await _context.SaveChangesAsync();
        return StoreOperationResult.Succeeded(0, "Category order updated.");
    }

    // ============================================================
    // Products
    // ============================================================

    public async Task<List<ProductViewModel>> GetProductsAsync(int? categoryId = null, ProductType? productType = null,
        bool activeOnly = true, bool featuredOnly = false, string? search = null,
        string sortBy = "name", int page = 1, int pageSize = 24)
    {
        var query = BuildProductQuery(categoryId, productType, activeOnly, featuredOnly, search);

        query = sortBy switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "newest" => query.OrderByDescending(p => p.CreatedDate),
            "featured" => query.OrderByDescending(p => p.IsFeatured).ThenBy(p => p.SortOrder).ThenBy(p => p.Name),
            _ => query.OrderBy(p => p.SortOrder).ThenBy(p => p.Name)
        };

        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => MapProductToViewModel(p))
            .ToListAsync();
    }

    public async Task<int> GetProductCountAsync(int? categoryId = null, ProductType? productType = null,
        bool activeOnly = true, string? search = null)
    {
        return await BuildProductQuery(categoryId, productType, activeOnly, false, search).CountAsync();
    }

    public async Task<ProductViewModel?> GetProductByIdAsync(int productId)
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .Include(p => p.DigitalFiles)
            .Include(p => p.Category)
            .Where(p => p.ProductID == productId)
            .Select(p => MapProductToViewModel(p))
            .FirstOrDefaultAsync();
    }

    public async Task<ProductViewModel?> GetProductBySlugAsync(string slug)
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .Include(p => p.DigitalFiles)
            .Include(p => p.Category)
            .Where(p => p.Slug == slug && p.IsActive)
            .Select(p => MapProductToViewModel(p))
            .FirstOrDefaultAsync();
    }

    public async Task<StoreOperationResult> CreateProductAsync(CreateProductModel model, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            return StoreOperationResult.Failed("Product name is required.");

        if (model.Price < 0)
            return StoreOperationResult.Failed("Price cannot be negative.");

        var slug = GenerateSlug(model.Name);
        var existingSlug = await _context.Products.AnyAsync(p => p.Slug == slug && p.IsActive);
        if (existingSlug)
            slug = $"{slug}-{DateTime.UtcNow.Ticks % 10000}";

        var sku = await GenerateSkuAsync(model.Name, model.ProductType);

        var product = new Product
        {
            Name = model.Name.Trim(),
            Slug = slug,
            SKU = sku,
            ProductType = model.ProductType,
            Description = model.Description?.Trim(),
            ShortDescription = model.ShortDescription?.Trim(),
            Price = model.Price,
            CompareAtPrice = model.CompareAtPrice,
            CostPrice = model.CostPrice,
            MemberPrice = model.MemberPrice,
            CategoryID = model.CategoryID,
            StockQuantity = model.StockQuantity,
            LowStockThreshold = model.LowStockThreshold,
            TrackInventory = model.TrackInventory,
            Weight = model.Weight,
            BillingInterval = model.BillingInterval?.Trim(),
            BillingIntervalCount = model.BillingIntervalCount,
            StripePriceId = model.StripePriceId?.Trim(),
            PayPalPlanId = model.PayPalPlanId?.Trim(),
            MaxDownloads = model.MaxDownloads,
            DownloadExpiryDays = model.DownloadExpiryDays,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created product '{ProductName}' (ID: {ProductId}, Type: {ProductType})", product.Name, product.ProductID, product.ProductType);
        return StoreOperationResult.Succeeded(product.ProductID, $"Product '{product.Name}' created.");
    }

    public async Task<StoreOperationResult> UpdateProductAsync(EditProductModel model)
    {
        var product = await _context.Products.FindAsync(model.ProductID);
        if (product == null)
            return StoreOperationResult.Failed("Product not found.");

        if (string.IsNullOrWhiteSpace(model.Name))
            return StoreOperationResult.Failed("Product name is required.");

        if (product.Name != model.Name.Trim())
        {
            var slug = GenerateSlug(model.Name);
            var existingSlug = await _context.Products.AnyAsync(p => p.Slug == slug && p.IsActive && p.ProductID != model.ProductID);
            if (existingSlug)
                slug = $"{slug}-{DateTime.UtcNow.Ticks % 10000}";
            product.Slug = slug;
        }

        if (!string.IsNullOrWhiteSpace(model.SKU) && model.SKU != product.SKU)
        {
            var existingSku = await _context.Products.AnyAsync(p => p.SKU == model.SKU && p.IsActive && p.ProductID != model.ProductID);
            if (existingSku)
                return StoreOperationResult.Failed($"SKU '{model.SKU}' is already in use.");
            product.SKU = model.SKU.Trim();
        }

        product.Name = model.Name.Trim();
        product.Description = model.Description?.Trim();
        product.ShortDescription = model.ShortDescription?.Trim();
        product.Price = model.Price;
        product.CompareAtPrice = model.CompareAtPrice;
        product.CostPrice = model.CostPrice;
        product.MemberPrice = model.MemberPrice;
        product.CategoryID = model.CategoryID;
        product.IsActive = model.IsActive;
        product.IsFeatured = model.IsFeatured;
        product.SortOrder = model.SortOrder;
        product.StockQuantity = model.StockQuantity;
        product.LowStockThreshold = model.LowStockThreshold;
        product.TrackInventory = model.TrackInventory;
        product.Weight = model.Weight;
        product.BillingInterval = model.BillingInterval?.Trim();
        product.BillingIntervalCount = model.BillingIntervalCount;
        product.StripePriceId = model.StripePriceId?.Trim();
        product.PayPalPlanId = model.PayPalPlanId?.Trim();
        product.MaxDownloads = model.MaxDownloads;
        product.DownloadExpiryDays = model.DownloadExpiryDays;
        product.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated product '{ProductName}' (ID: {ProductId})", product.Name, product.ProductID);
        return StoreOperationResult.Succeeded(product.ProductID, $"Product '{product.Name}' updated.");
    }

    public async Task<StoreOperationResult> DeleteProductAsync(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
            return StoreOperationResult.Failed("Product not found.");

        product.IsActive = false;
        product.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Soft-deleted product '{ProductName}' (ID: {ProductId})", product.Name, product.ProductID);
        return StoreOperationResult.SucceededNoId($"Product '{product.Name}' deactivated.");
    }

    // ============================================================
    // Product Images
    // ============================================================

    public async Task<StoreOperationResult> UploadProductImagesAsync(int productId, IFormFile[] files)
    {
        var product = await _context.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.ProductID == productId);
        if (product == null)
            return StoreOperationResult.Failed("Product not found.");

        Directory.CreateDirectory(_productImagesPath);

        int uploaded = 0;
        bool isFirst = !product.Images.Any();
        int maxSort = product.Images.Any() ? product.Images.Max(i => i.SortOrder) : 0;

        foreach (var file in files)
        {
            if (!_imageOptimization.IsValidImageFormat(file))
                continue;

            var fileName = $"prod_{productId}_{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";

            using var stream = file.OpenReadStream();

            // Full-size image
            var optimized = await _imageOptimization.OptimizeImageAsync(stream, 1200);
            await File.WriteAllBytesAsync(Path.Combine(_productImagesPath, fileName), optimized);

            // Thumbnail
            stream.Position = 0;
            var thumbnail = await _imageOptimization.GenerateThumbnailAsync(stream, 400);
            var thumbName = $"{Path.GetFileNameWithoutExtension(fileName)}_thumb{Path.GetExtension(fileName)}";
            await File.WriteAllBytesAsync(Path.Combine(_productImagesPath, thumbName), thumbnail);

            maxSort++;
            var image = new ProductImage
            {
                ProductID = productId,
                FileName = fileName,
                OriginalFileName = file.FileName,
                AltText = product.Name,
                SortOrder = maxSort,
                IsPrimary = isFirst && uploaded == 0,
                UploadedDate = DateTime.UtcNow
            };

            _context.ProductImages.Add(image);
            uploaded++;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Uploaded {Count} images for product ID {ProductId}", uploaded, productId);
        return StoreOperationResult.Succeeded(productId, $"{uploaded} image(s) uploaded.");
    }

    public async Task<StoreOperationResult> SetPrimaryImageAsync(int productId, int imageId)
    {
        var images = await _context.ProductImages.Where(i => i.ProductID == productId).ToListAsync();
        if (!images.Any())
            return StoreOperationResult.Failed("No images found for this product.");

        foreach (var img in images)
            img.IsPrimary = img.ImageID == imageId;

        await _context.SaveChangesAsync();
        return StoreOperationResult.SucceededNoId("Primary image updated.");
    }

    public async Task<StoreOperationResult> DeleteProductImageAsync(int imageId)
    {
        var image = await _context.ProductImages.FindAsync(imageId);
        if (image == null)
            return StoreOperationResult.Failed("Image not found.");

        // Delete files
        var fullPath = Path.Combine(_productImagesPath, image.FileName);
        if (File.Exists(fullPath)) File.Delete(fullPath);

        var thumbPath = Path.Combine(_productImagesPath, image.ThumbnailFileName);
        if (File.Exists(thumbPath)) File.Delete(thumbPath);

        _context.ProductImages.Remove(image);
        await _context.SaveChangesAsync();

        return StoreOperationResult.SucceededNoId("Image deleted.");
    }

    // ============================================================
    // Digital Files
    // ============================================================

    public async Task<StoreOperationResult> UploadDigitalFileAsync(int productId, IFormFile file, string uploadedBy)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
            return StoreOperationResult.Failed("Product not found.");

        if (product.ProductType != ProductType.Digital)
            return StoreOperationResult.Failed("Digital files can only be uploaded for digital products.");

        Directory.CreateDirectory(_digitalFilesPath);

        var fileName = $"dig_{productId}_{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
        var filePath = Path.Combine(_digitalFilesPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var digitalFile = new DigitalProductFile
        {
            ProductID = productId,
            FileName = fileName,
            OriginalFileName = file.FileName,
            FileSize = file.Length,
            ContentType = file.ContentType,
            UploadedDate = DateTime.UtcNow,
            UploadedBy = uploadedBy
        };

        _context.DigitalProductFiles.Add(digitalFile);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Uploaded digital file for product ID {ProductId}: {FileName}", productId, file.FileName);
        return StoreOperationResult.Succeeded(digitalFile.FileID, $"File '{file.FileName}' uploaded.");
    }

    public async Task<StoreOperationResult> DeleteDigitalFileAsync(int fileId)
    {
        var file = await _context.DigitalProductFiles.FindAsync(fileId);
        if (file == null)
            return StoreOperationResult.Failed("File not found.");

        var filePath = Path.Combine(_digitalFilesPath, file.FileName);
        if (File.Exists(filePath)) File.Delete(filePath);

        _context.DigitalProductFiles.Remove(file);
        await _context.SaveChangesAsync();

        return StoreOperationResult.SucceededNoId("Digital file deleted.");
    }

    // ============================================================
    // Inventory
    // ============================================================

    public async Task<StoreOperationResult> AdjustStockAsync(int productId, int adjustment, string reason)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
            return StoreOperationResult.Failed("Product not found.");

        if (product.ProductType != ProductType.Physical)
            return StoreOperationResult.Failed("Stock can only be adjusted for physical products.");

        var oldStock = product.StockQuantity;
        product.StockQuantity += adjustment;
        if (product.StockQuantity < 0)
            product.StockQuantity = 0;

        product.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Stock adjusted for product '{ProductName}' (ID: {ProductId}): {OldStock} -> {NewStock}. Reason: {Reason}",
            product.Name, product.ProductID, oldStock, product.StockQuantity, reason);

        return StoreOperationResult.Succeeded(productId, $"Stock adjusted from {oldStock} to {product.StockQuantity}.");
    }

    public async Task<List<ProductViewModel>> GetLowStockProductsAsync()
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .Where(p => p.IsActive && p.ProductType == ProductType.Physical && p.TrackInventory && p.StockQuantity <= p.LowStockThreshold)
            .OrderBy(p => p.StockQuantity)
            .Select(p => MapProductToViewModel(p))
            .ToListAsync();
    }

    // ============================================================
    // Storefront
    // ============================================================

    public async Task<StoreHomeViewModel> GetStoreHomeAsync(bool isMember)
    {
        var featured = await _context.Products
            .AsNoTracking()
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.IsFeatured && p.ProductType != ProductType.Subscription)
            .OrderBy(p => p.SortOrder)
            .Take(8)
            .Select(p => MapProductToViewModel(p))
            .ToListAsync();

        var categories = await GetCategoriesAsync(null);

        var subscriptions = await _context.Products
            .AsNoTracking()
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .Where(p => p.IsActive && p.ProductType == ProductType.Subscription)
            .OrderBy(p => p.SortOrder)
            .Take(4)
            .Select(p => MapProductToViewModel(p))
            .ToListAsync();

        if (isMember)
        {
            ApplyMemberPricing(featured);
            ApplyMemberPricing(subscriptions);
        }

        return new StoreHomeViewModel
        {
            FeaturedProducts = featured,
            Categories = categories,
            SubscriptionProducts = subscriptions
        };
    }

    public async Task<StoreBrowseViewModel> BuildBrowseViewModelAsync(int? categoryId, ProductType? productType,
        string? search, string sortBy, int page, int pageSize, bool isMember)
    {
        if (page < 1) page = 1;
        if (pageSize < 12) pageSize = 12;
        if (pageSize > 48) pageSize = 48;

        var totalProducts = await GetProductCountAsync(categoryId, productType, true, search);
        var totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

        var products = await GetProductsAsync(categoryId, productType, true, false, search, sortBy, page, pageSize);
        if (isMember) ApplyMemberPricing(products);

        var categories = await GetCategoriesAsync(categoryId);
        var categoryTree = await GetCategoryTreeAsync(categoryId);
        var breadcrumbs = await GetBreadcrumbsAsync(categoryId);

        string categoryName = "Shop";
        string? categorySlug = null;
        string? categoryDescription = null;

        if (categoryId.HasValue)
        {
            var cat = await GetCategoryByIdAsync(categoryId.Value);
            if (cat != null)
            {
                categoryName = cat.Name;
                categorySlug = cat.Slug;
                categoryDescription = cat.Description;
            }
        }

        return new StoreBrowseViewModel
        {
            CurrentCategoryId = categoryId,
            CurrentCategoryName = categoryName,
            CurrentCategorySlug = categorySlug,
            CurrentCategoryDescription = categoryDescription,
            Breadcrumbs = breadcrumbs,
            Categories = categories,
            Products = products,
            CategoryTree = categoryTree,
            FilterProductType = productType,
            SearchQuery = search,
            SortBy = sortBy,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            TotalProducts = totalProducts
        };
    }

    public async Task<ProductDetailViewModel?> GetProductDetailAsync(string slug, bool isMember)
    {
        var product = await GetProductBySlugAsync(slug);
        if (product == null)
            return null;

        if (isMember)
            ApplyMemberPricing([product]);

        var relatedProducts = await _context.Products
            .AsNoTracking()
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .Where(p => p.IsActive && p.CategoryID == product.CategoryId && p.ProductID != product.ProductId)
            .OrderBy(p => p.SortOrder)
            .Take(4)
            .Select(p => MapProductToViewModel(p))
            .ToListAsync();

        if (isMember) ApplyMemberPricing(relatedProducts);

        var breadcrumbs = await GetBreadcrumbsAsync(product.CategoryId);
        breadcrumbs.Add(new StoreBreadcrumbItem { Name = product.Name, IsCurrent = true });

        return new ProductDetailViewModel
        {
            Product = product,
            RelatedProducts = relatedProducts,
            Breadcrumbs = breadcrumbs
        };
    }

    public async Task<List<StoreBreadcrumbItem>> GetBreadcrumbsAsync(int? categoryId)
    {
        var breadcrumbs = new List<StoreBreadcrumbItem>
        {
            new() { Name = "Shop", Slug = null, IsCurrent = !categoryId.HasValue }
        };

        if (!categoryId.HasValue)
            return breadcrumbs;

        var trail = new List<StoreBreadcrumbItem>();
        var currentId = categoryId;

        while (currentId.HasValue)
        {
            var cat = await _context.StoreCategories
                .AsNoTracking()
                .Where(c => c.CategoryID == currentId)
                .Select(c => new { c.CategoryID, c.CategoryName, c.Slug, c.ParentCategoryID })
                .FirstOrDefaultAsync();

            if (cat == null) break;

            trail.Add(new StoreBreadcrumbItem
            {
                CategoryId = cat.CategoryID,
                Slug = cat.Slug,
                Name = cat.CategoryName,
                IsCurrent = cat.CategoryID == categoryId
            });

            currentId = cat.ParentCategoryID;
        }

        trail.Reverse();
        breadcrumbs.AddRange(trail);
        return breadcrumbs;
    }

    // ============================================================
    // Helpers
    // ============================================================

    public string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant().Trim();
        slug = SlugInvalidCharsRegex().Replace(slug, "");
        slug = SlugWhitespaceRegex().Replace(slug, "-");
        slug = slug.Trim('-');
        return string.IsNullOrEmpty(slug) ? "product" : slug;
    }

    [GeneratedRegex(@"[^a-z0-9\s-]")]
    private static partial Regex SlugInvalidCharsRegex();

    [GeneratedRegex(@"[\s-]+")]
    private static partial Regex SlugWhitespaceRegex();

    private async Task<string> GenerateSkuAsync(string name, ProductType productType)
    {
        var prefix = productType switch
        {
            ProductType.Physical => "PHY",
            ProductType.Digital => "DIG",
            ProductType.Subscription => "SUB",
            _ => "PRD"
        };

        var namePrefix = SlugInvalidCharsRegex().Replace(name.ToUpperInvariant(), "");
        if (namePrefix.Length > 4)
            namePrefix = namePrefix[..4];

        var count = await _context.Products.CountAsync() + 1;
        var sku = $"{prefix}-{namePrefix}-{count:D4}";

        while (await _context.Products.AnyAsync(p => p.SKU == sku))
        {
            count++;
            sku = $"{prefix}-{namePrefix}-{count:D4}";
        }

        return sku;
    }

    private IQueryable<Product> BuildProductQuery(int? categoryId = null, ProductType? productType = null,
        bool activeOnly = true, bool featuredOnly = false, string? search = null)
    {
        var query = _context.Products.AsNoTracking()
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .Include(p => p.Category)
            .AsQueryable();

        if (activeOnly)
            query = query.Where(p => p.IsActive);

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryID == categoryId);

        if (productType.HasValue)
            query = query.Where(p => p.ProductType == productType);

        if (featuredOnly)
            query = query.Where(p => p.IsFeatured);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) ||
                                     (p.Description != null && p.Description.ToLower().Contains(term)) ||
                                     p.SKU.ToLower().Contains(term));
        }

        return query;
    }

    private static ProductViewModel MapProductToViewModel(Product p)
    {
        var primaryImage = p.Images.FirstOrDefault(i => i.IsPrimary) ?? p.Images.FirstOrDefault();
        return new ProductViewModel
        {
            ProductId = p.ProductID,
            Name = p.Name,
            Slug = p.Slug,
            SKU = p.SKU,
            ProductType = p.ProductType,
            Description = p.Description,
            ShortDescription = p.ShortDescription,
            Price = p.Price,
            CompareAtPrice = p.CompareAtPrice,
            CostPrice = p.CostPrice,
            MemberPrice = p.MemberPrice,
            DisplayPrice = p.Price,
            CategoryId = p.CategoryID,
            CategoryName = p.Category?.CategoryName,
            CategorySlug = p.Category?.Slug,
            StockQuantity = p.StockQuantity,
            LowStockThreshold = p.LowStockThreshold,
            TrackInventory = p.TrackInventory,
            Weight = p.Weight,
            MaxDownloads = p.MaxDownloads,
            DownloadExpiryDays = p.DownloadExpiryDays,
            BillingInterval = p.BillingInterval,
            BillingIntervalCount = p.BillingIntervalCount,
            StripePriceId = p.StripePriceId,
            PayPalPlanId = p.PayPalPlanId,
            IsActive = p.IsActive,
            IsFeatured = p.IsFeatured,
            SortOrder = p.SortOrder,
            CreatedDate = p.CreatedDate,
            CreatedBy = p.CreatedBy,
            PrimaryImageUrl = primaryImage?.ImageUrl,
            PrimaryThumbnailUrl = primaryImage?.ThumbnailUrl,
            Images = p.Images.Select(i => new ProductImageViewModel
            {
                ImageId = i.ImageID,
                FileName = i.FileName,
                OriginalFileName = i.OriginalFileName,
                AltText = i.AltText,
                SortOrder = i.SortOrder,
                IsPrimary = i.IsPrimary
            }).ToList(),
            DigitalFiles = p.DigitalFiles.Select(f => new DigitalFileViewModel
            {
                FileId = f.FileID,
                FileName = f.FileName,
                OriginalFileName = f.OriginalFileName,
                FileSize = f.FileSize,
                ContentType = f.ContentType,
                UploadedDate = f.UploadedDate
            }).ToList()
        };
    }

    private static void ApplyMemberPricing(List<ProductViewModel> products)
    {
        foreach (var p in products)
        {
            if (p.MemberPrice.HasValue && p.MemberPrice < p.Price)
            {
                p.DisplayPrice = p.MemberPrice.Value;
                p.ShowMemberPrice = true;
            }
        }
    }
}
