namespace Ape.Models.ViewModels
{
    /// <summary>
    /// View model for displaying a link category
    /// </summary>
    public class LinkCategoryViewModel
    {
        public int CategoryId { get; set; }
        public required string CategoryName { get; set; }
        public int SortOrder { get; set; }
        public bool IsAdminOnly { get; set; }
        public int LinkCount { get; set; }
        public List<LinkViewModel> Links { get; set; } = [];
    }

    /// <summary>
    /// View model for displaying a link
    /// </summary>
    public class LinkViewModel
    {
        public int LinkId { get; set; }
        public int CategoryId { get; set; }
        public required string LinkName { get; set; }
        public required string LinkUrl { get; set; }
        public int SortOrder { get; set; }
    }

    /// <summary>
    /// Model for creating a new category
    /// </summary>
    public class CreateLinkCategoryModel
    {
        public required string CategoryName { get; set; }
        public bool IsAdminOnly { get; set; }
    }

    /// <summary>
    /// Model for creating a new link
    /// </summary>
    public class CreateLinkModel
    {
        public int CategoryId { get; set; }
        public required string LinkName { get; set; }
        public required string LinkUrl { get; set; }
    }

    /// <summary>
    /// Model for updating a link
    /// </summary>
    public class UpdateLinkModel
    {
        public int LinkId { get; set; }
        public required string LinkName { get; set; }
        public required string LinkUrl { get; set; }
    }

    /// <summary>
    /// Result object for category operations
    /// </summary>
    public class LinkCategoryOperationResult
    {
        public bool Success { get; set; }
        public int? CategoryId { get; set; }
        public string? Message { get; set; }

        public static LinkCategoryOperationResult Succeeded(int categoryId, string? message = null)
            => new() { Success = true, CategoryId = categoryId, Message = message };

        public static LinkCategoryOperationResult Failed(string message)
            => new() { Success = false, Message = message };
    }

    /// <summary>
    /// Result object for link operations
    /// </summary>
    public class LinkOperationResult
    {
        public bool Success { get; set; }
        public int? LinkId { get; set; }
        public int? CategoryId { get; set; }
        public string? Message { get; set; }

        public static LinkOperationResult Succeeded(int linkId, int categoryId, string? message = null)
            => new() { Success = true, LinkId = linkId, CategoryId = categoryId, Message = message };

        public static LinkOperationResult Failed(string message)
            => new() { Success = false, Message = message };
    }

    /// <summary>
    /// Composite view model for the manage links page
    /// </summary>
    public class ManageLinksViewModel
    {
        public List<LinkCategoryViewModel> Categories { get; set; } = [];
        public int? SelectedCategoryId { get; set; }
        public string? SelectedCategoryName { get; set; }
        public List<LinkViewModel> Links { get; set; } = [];
    }
}
