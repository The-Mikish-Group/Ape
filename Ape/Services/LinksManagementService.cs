using Ape.Data;
using Ape.Models;
using Ape.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Ape.Services
{
    /// <summary>
    /// Service for managing link categories and links
    /// </summary>
    public class LinksManagementService(
        ApplicationDbContext context,
        ILogger<LinksManagementService> logger) : ILinksManagementService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<LinksManagementService> _logger = logger;

        // ============================================================
        // Category Operations
        // ============================================================

        public async Task<List<LinkCategoryViewModel>> GetCategoriesAsync(bool includeAdminOnly)
        {
            var query = _context.LinkCategories.AsNoTracking();

            if (!includeAdminOnly)
            {
                query = query.Where(c => !c.IsAdminOnly);
            }

            var categories = await query
                .Include(c => c.CategoryLinks)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.CategoryName)
                .ToListAsync();

            return categories.Select(c => new LinkCategoryViewModel
            {
                CategoryId = c.CategoryID,
                CategoryName = c.CategoryName,
                SortOrder = c.SortOrder,
                IsAdminOnly = c.IsAdminOnly,
                LinkCount = c.CategoryLinks.Count,
                Links = c.CategoryLinks
                    .OrderBy(l => l.SortOrder)
                    .ThenBy(l => l.LinkName)
                    .Select(l => new LinkViewModel
                    {
                        LinkId = l.LinkID,
                        CategoryId = l.CategoryID,
                        LinkName = l.LinkName,
                        LinkUrl = l.LinkUrl,
                        SortOrder = l.SortOrder
                    }).ToList()
            }).ToList();
        }

        public async Task<LinkCategoryViewModel?> GetCategoryAsync(int categoryId)
        {
            var category = await _context.LinkCategories
                .AsNoTracking()
                .Include(c => c.CategoryLinks)
                .FirstOrDefaultAsync(c => c.CategoryID == categoryId);

            if (category == null)
                return null;

            return new LinkCategoryViewModel
            {
                CategoryId = category.CategoryID,
                CategoryName = category.CategoryName,
                SortOrder = category.SortOrder,
                IsAdminOnly = category.IsAdminOnly,
                LinkCount = category.CategoryLinks.Count,
                Links = category.CategoryLinks
                    .OrderBy(l => l.SortOrder)
                    .ThenBy(l => l.LinkName)
                    .Select(l => new LinkViewModel
                    {
                        LinkId = l.LinkID,
                        CategoryId = l.CategoryID,
                        LinkName = l.LinkName,
                        LinkUrl = l.LinkUrl,
                        SortOrder = l.SortOrder
                    }).ToList()
            };
        }

        public async Task<LinkCategoryOperationResult> CreateCategoryAsync(CreateLinkCategoryModel model)
        {
            if (string.IsNullOrWhiteSpace(model.CategoryName))
            {
                return LinkCategoryOperationResult.Failed("Category name is required.");
            }

            var trimmedName = model.CategoryName.Trim();

            // Check if category already exists
            var existingCategory = await _context.LinkCategories
                .FirstOrDefaultAsync(c => c.CategoryName == trimmedName);

            if (existingCategory != null)
            {
                return LinkCategoryOperationResult.Failed($"A category named '{trimmedName}' already exists.");
            }

            // Get next sort order
            var maxSortOrder = await _context.LinkCategories
                .MaxAsync(c => (int?)c.SortOrder) ?? 0;

            var category = new LinkCategory
            {
                CategoryName = trimmedName,
                SortOrder = maxSortOrder + 1,
                IsAdminOnly = model.IsAdminOnly
            };

            _context.LinkCategories.Add(category);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created link category '{CategoryName}' with ID {CategoryId}",
                trimmedName, category.CategoryID);

            return LinkCategoryOperationResult.Succeeded(category.CategoryID,
                $"Category '{trimmedName}' created successfully.");
        }

        public async Task<LinkCategoryOperationResult> UpdateCategoryAsync(int categoryId, string categoryName, bool isAdminOnly)
        {
            var category = await _context.LinkCategories.FirstOrDefaultAsync(c => c.CategoryID == categoryId);

            if (category == null)
            {
                return LinkCategoryOperationResult.Failed("Category not found.");
            }

            if (string.IsNullOrWhiteSpace(categoryName))
            {
                return LinkCategoryOperationResult.Failed("Category name is required.");
            }

            var trimmedName = categoryName.Trim();

            // Check for duplicate name (excluding current category)
            var existingCategory = await _context.LinkCategories
                .FirstOrDefaultAsync(c => c.CategoryName == trimmedName && c.CategoryID != categoryId);

            if (existingCategory != null)
            {
                return LinkCategoryOperationResult.Failed($"A category named '{trimmedName}' already exists.");
            }

            category.CategoryName = trimmedName;
            category.IsAdminOnly = isAdminOnly;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated link category {CategoryId} to '{CategoryName}'",
                categoryId, trimmedName);

            return LinkCategoryOperationResult.Succeeded(categoryId,
                $"Category '{trimmedName}' updated successfully.");
        }

        public async Task<LinkCategoryOperationResult> DeleteCategoryAsync(int categoryId)
        {
            var category = await _context.LinkCategories
                .Include(c => c.CategoryLinks)
                .FirstOrDefaultAsync(c => c.CategoryID == categoryId);

            if (category == null)
            {
                return LinkCategoryOperationResult.Failed("Category not found.");
            }

            var categoryName = category.CategoryName;
            var linkCount = category.CategoryLinks.Count;

            _context.LinkCategories.Remove(category);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted link category '{CategoryName}' with {LinkCount} links",
                categoryName, linkCount);

            return LinkCategoryOperationResult.Succeeded(categoryId,
                $"Category '{categoryName}' and all its links deleted successfully.");
        }

        public async Task<LinkCategoryOperationResult> MoveCategoryAsync(int categoryId, string direction)
        {
            var category = await _context.LinkCategories.FirstOrDefaultAsync(c => c.CategoryID == categoryId);
            if (category == null)
            {
                return LinkCategoryOperationResult.Failed("Category not found.");
            }

            var allCategories = await _context.LinkCategories
                .OrderBy(c => c.SortOrder)
                .ToListAsync();

            var currentIndex = allCategories.FindIndex(c => c.CategoryID == categoryId);

            if (direction == "up" && currentIndex > 0)
            {
                (category.SortOrder, allCategories[currentIndex - 1].SortOrder) =
                    (allCategories[currentIndex - 1].SortOrder, category.SortOrder);
            }
            else if (direction == "down" && currentIndex < allCategories.Count - 1)
            {
                (category.SortOrder, allCategories[currentIndex + 1].SortOrder) =
                    (allCategories[currentIndex + 1].SortOrder, category.SortOrder);
            }

            await _context.SaveChangesAsync();

            return LinkCategoryOperationResult.Succeeded(categoryId, "Category moved successfully.");
        }

        public async Task<LinkCategoryOperationResult> UpdateCategorySortOrdersAsync(int[] categoryIds, int[] sortOrders)
        {
            if (categoryIds == null || sortOrders == null || categoryIds.Length != sortOrders.Length)
            {
                return LinkCategoryOperationResult.Failed("Invalid data provided.");
            }

            for (int i = 0; i < categoryIds.Length; i++)
            {
                var category = await _context.LinkCategories.FirstOrDefaultAsync(c => c.CategoryID == categoryIds[i]);
                if (category != null)
                {
                    category.SortOrder = sortOrders[i];
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated sort order for {Count} categories", categoryIds.Length);

            return LinkCategoryOperationResult.Succeeded(0, "Categories reordered successfully.");
        }

        // ============================================================
        // Link Operations
        // ============================================================

        public async Task<List<LinkViewModel>> GetLinksAsync(int categoryId)
        {
            var links = await _context.CategoryLinks
                .AsNoTracking()
                .Where(l => l.CategoryID == categoryId)
                .OrderBy(l => l.SortOrder)
                .ThenBy(l => l.LinkName)
                .ToListAsync();

            return links.Select(l => new LinkViewModel
            {
                LinkId = l.LinkID,
                CategoryId = l.CategoryID,
                LinkName = l.LinkName,
                LinkUrl = l.LinkUrl,
                SortOrder = l.SortOrder
            }).ToList();
        }

        public async Task<LinkViewModel?> GetLinkAsync(int linkId)
        {
            var link = await _context.CategoryLinks
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.LinkID == linkId);

            if (link == null)
                return null;

            return new LinkViewModel
            {
                LinkId = link.LinkID,
                CategoryId = link.CategoryID,
                LinkName = link.LinkName,
                LinkUrl = link.LinkUrl,
                SortOrder = link.SortOrder
            };
        }

        public async Task<LinkOperationResult> CreateLinkAsync(CreateLinkModel model)
        {
            if (string.IsNullOrWhiteSpace(model.LinkName) || string.IsNullOrWhiteSpace(model.LinkUrl))
            {
                return LinkOperationResult.Failed("Both link name and URL are required.");
            }

            // Validate URL format
            if (!Uri.TryCreate(model.LinkUrl, UriKind.Absolute, out _))
            {
                return LinkOperationResult.Failed("Please enter a valid URL.");
            }

            // Verify category exists
            var category = await _context.LinkCategories.FirstOrDefaultAsync(c => c.CategoryID == model.CategoryId);
            if (category == null)
            {
                return LinkOperationResult.Failed("Selected category not found.");
            }

            // Get next sort order for this category
            var maxSortOrder = await _context.CategoryLinks
                .Where(l => l.CategoryID == model.CategoryId)
                .MaxAsync(l => (int?)l.SortOrder) ?? 0;

            var link = new CategoryLink
            {
                CategoryID = model.CategoryId,
                LinkName = model.LinkName.Trim(),
                LinkUrl = model.LinkUrl.Trim(),
                SortOrder = maxSortOrder + 1
            };

            _context.CategoryLinks.Add(link);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created link '{LinkName}' in category {CategoryId}",
                link.LinkName, model.CategoryId);

            return LinkOperationResult.Succeeded(link.LinkID, model.CategoryId,
                $"Link '{link.LinkName}' added successfully.");
        }

        public async Task<LinkOperationResult> UpdateLinkAsync(UpdateLinkModel model)
        {
            var link = await _context.CategoryLinks.FirstOrDefaultAsync(l => l.LinkID == model.LinkId);

            if (link == null)
            {
                return LinkOperationResult.Failed("Link not found.");
            }

            if (string.IsNullOrWhiteSpace(model.LinkName) || string.IsNullOrWhiteSpace(model.LinkUrl))
            {
                return LinkOperationResult.Failed("Both link name and URL are required.");
            }

            // Validate URL format
            if (!Uri.TryCreate(model.LinkUrl, UriKind.Absolute, out _))
            {
                return LinkOperationResult.Failed("Please enter a valid URL.");
            }

            link.LinkName = model.LinkName.Trim();
            link.LinkUrl = model.LinkUrl.Trim();
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated link {LinkId} to '{LinkName}'", model.LinkId, link.LinkName);

            return LinkOperationResult.Succeeded(model.LinkId, link.CategoryID,
                $"Link '{link.LinkName}' updated successfully.");
        }

        public async Task<LinkOperationResult> DeleteLinkAsync(int linkId)
        {
            var link = await _context.CategoryLinks.FirstOrDefaultAsync(l => l.LinkID == linkId);

            if (link == null)
            {
                return LinkOperationResult.Failed("Link not found.");
            }

            var linkName = link.LinkName;
            var categoryId = link.CategoryID;

            _context.CategoryLinks.Remove(link);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted link '{LinkName}' from category {CategoryId}", linkName, categoryId);

            return LinkOperationResult.Succeeded(linkId, categoryId,
                $"Link '{linkName}' deleted successfully.");
        }

        public async Task<LinkOperationResult> MoveLinkAsync(int linkId, string direction)
        {
            var link = await _context.CategoryLinks.FirstOrDefaultAsync(l => l.LinkID == linkId);
            if (link == null)
            {
                return LinkOperationResult.Failed("Link not found.");
            }

            var categoryLinks = await _context.CategoryLinks
                .Where(l => l.CategoryID == link.CategoryID)
                .OrderBy(l => l.SortOrder)
                .ToListAsync();

            var currentIndex = categoryLinks.FindIndex(l => l.LinkID == linkId);

            if (direction == "up" && currentIndex > 0)
            {
                (link.SortOrder, categoryLinks[currentIndex - 1].SortOrder) =
                    (categoryLinks[currentIndex - 1].SortOrder, link.SortOrder);
            }
            else if (direction == "down" && currentIndex < categoryLinks.Count - 1)
            {
                (link.SortOrder, categoryLinks[currentIndex + 1].SortOrder) =
                    (categoryLinks[currentIndex + 1].SortOrder, link.SortOrder);
            }

            await _context.SaveChangesAsync();

            return LinkOperationResult.Succeeded(linkId, link.CategoryID, "Link moved successfully.");
        }

        public async Task<LinkOperationResult> UpdateLinkSortOrdersAsync(int[] linkIds, int[] sortOrders)
        {
            if (linkIds == null || sortOrders == null || linkIds.Length != sortOrders.Length)
            {
                return LinkOperationResult.Failed("Invalid data provided.");
            }

            int? categoryId = null;

            for (int i = 0; i < linkIds.Length; i++)
            {
                var link = await _context.CategoryLinks.FirstOrDefaultAsync(l => l.LinkID == linkIds[i]);
                if (link != null)
                {
                    link.SortOrder = sortOrders[i];
                    categoryId ??= link.CategoryID;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated sort order for {Count} links", linkIds.Length);

            return LinkOperationResult.Succeeded(0, categoryId ?? 0, "Links reordered successfully.");
        }

        // ============================================================
        // View Models
        // ============================================================

        public async Task<ManageLinksViewModel> BuildManageLinksViewModelAsync(int? selectedCategoryId)
        {
            var categories = await GetCategoriesAsync(includeAdminOnly: true);

            var viewModel = new ManageLinksViewModel
            {
                Categories = categories,
                SelectedCategoryId = selectedCategoryId,
                Links = []
            };

            if (selectedCategoryId.HasValue)
            {
                var selectedCategory = categories.FirstOrDefault(c => c.CategoryId == selectedCategoryId.Value);
                if (selectedCategory != null)
                {
                    viewModel.SelectedCategoryName = selectedCategory.CategoryName;
                    viewModel.Links = await GetLinksAsync(selectedCategoryId.Value);
                }
            }

            return viewModel;
        }
    }
}
