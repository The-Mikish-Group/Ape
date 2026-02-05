using Ape.Models.ViewModels;

namespace Ape.Services
{
    /// <summary>
    /// Service interface for links management operations
    /// </summary>
    public interface ILinksManagementService
    {
        // ============================================================
        // Category Operations
        // ============================================================

        /// <summary>
        /// Get all categories with their links, filtered by user access
        /// </summary>
        Task<List<LinkCategoryViewModel>> GetCategoriesAsync(bool includeAdminOnly);

        /// <summary>
        /// Get a single category by ID
        /// </summary>
        Task<LinkCategoryViewModel?> GetCategoryAsync(int categoryId);

        /// <summary>
        /// Create a new category
        /// </summary>
        Task<LinkCategoryOperationResult> CreateCategoryAsync(CreateLinkCategoryModel model);

        /// <summary>
        /// Update a category's name and admin-only status
        /// </summary>
        Task<LinkCategoryOperationResult> UpdateCategoryAsync(int categoryId, string categoryName, bool isAdminOnly);

        /// <summary>
        /// Delete a category and all its links
        /// </summary>
        Task<LinkCategoryOperationResult> DeleteCategoryAsync(int categoryId);

        /// <summary>
        /// Move a category up or down in sort order
        /// </summary>
        Task<LinkCategoryOperationResult> MoveCategoryAsync(int categoryId, string direction);

        /// <summary>
        /// Update category sort orders (for drag-and-drop reordering)
        /// </summary>
        Task<LinkCategoryOperationResult> UpdateCategorySortOrdersAsync(int[] categoryIds, int[] sortOrders);

        // ============================================================
        // Link Operations
        // ============================================================

        /// <summary>
        /// Get all links in a category
        /// </summary>
        Task<List<LinkViewModel>> GetLinksAsync(int categoryId);

        /// <summary>
        /// Get a single link by ID
        /// </summary>
        Task<LinkViewModel?> GetLinkAsync(int linkId);

        /// <summary>
        /// Create a new link
        /// </summary>
        Task<LinkOperationResult> CreateLinkAsync(CreateLinkModel model);

        /// <summary>
        /// Update a link's name and URL
        /// </summary>
        Task<LinkOperationResult> UpdateLinkAsync(UpdateLinkModel model);

        /// <summary>
        /// Delete a link
        /// </summary>
        Task<LinkOperationResult> DeleteLinkAsync(int linkId);

        /// <summary>
        /// Move a link up or down in sort order within its category
        /// </summary>
        Task<LinkOperationResult> MoveLinkAsync(int linkId, string direction);

        /// <summary>
        /// Update link sort orders (for drag-and-drop reordering)
        /// </summary>
        Task<LinkOperationResult> UpdateLinkSortOrdersAsync(int[] linkIds, int[] sortOrders);

        // ============================================================
        // View Models
        // ============================================================

        /// <summary>
        /// Build the view model for the manage links page
        /// </summary>
        Task<ManageLinksViewModel> BuildManageLinksViewModelAsync(int? selectedCategoryId);
    }
}
