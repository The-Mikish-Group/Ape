using Ape.Models.ViewModels;
using Ape.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ape.Controllers
{
    public class LinksController(
        ILinksManagementService linksService,
        ILogger<LinksController> logger) : Controller
    {
        private readonly ILinksManagementService _linksService = linksService;
        private readonly ILogger<LinksController> _logger = logger;

        // GET: /Links/MoreLinks - View showing all categories and links in columns (Public access)
        [AllowAnonymous]
        public async Task<IActionResult> MoreLinks()
        {
            try
            {
                var includeAdminOnly = User.IsInRole("Admin") || User.IsInRole("Manager");
                var categories = await _linksService.GetCategoriesAsync(includeAdminOnly);

                ViewData["Title"] = "More Links";
                return View(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading More Links page");
                ViewData["Title"] = "More Links";
                ViewBag.DatabaseError = "The More Links system is not yet configured. Please run the database migration first.";
                ViewBag.ErrorDetails = ex.Message;
                return View(new List<LinkCategoryViewModel>());
            }
        }

        // GET: /Links/ManageCategories - Admin only
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ManageCategories()
        {
            try
            {
                var categories = await _linksService.GetCategoriesAsync(includeAdminOnly: true);

                ViewData["Title"] = "Manage Link Categories";
                return View(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Manage Categories page");
                ViewData["Title"] = "Manage Link Categories";
                TempData["ErrorMessage"] = $"Database error: {ex.Message}";
                return View(new List<LinkCategoryViewModel>());
            }
        }

        // GET: /Links/ManageLinks/{categoryId} - Admin only
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ManageLinks(int? categoryId)
        {
            var viewModel = await _linksService.BuildManageLinksViewModelAsync(categoryId);

            ViewBag.LinkCategories = viewModel.Categories;
            ViewBag.SelectedCategoryId = viewModel.SelectedCategoryId;
            ViewBag.SelectedCategoryName = viewModel.SelectedCategoryName;
            ViewData["Title"] = "Manage Category Links";

            if (categoryId.HasValue && viewModel.SelectedCategoryName == null)
            {
                return NotFound($"Category with ID {categoryId} not found.");
            }

            return View(viewModel.Links);
        }

        // POST: /Links/CreateCategory - Admin only
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(string categoryName, bool isAdminOnly = false)
        {
            var result = await _linksService.CreateCategoryAsync(new CreateLinkCategoryModel
            {
                CategoryName = categoryName,
                IsAdminOnly = isAdminOnly
            });

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(ManageCategories));
        }

        // POST: /Links/CreateLink - Admin only
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLink(int categoryId, string linkName, string linkUrl)
        {
            var result = await _linksService.CreateLinkAsync(new CreateLinkModel
            {
                CategoryId = categoryId,
                LinkName = linkName,
                LinkUrl = linkUrl
            });

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(ManageLinks), new { categoryId });
        }

        // POST: /Links/UpdateCategorySortOrder - Admin only
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCategorySortOrder(int categoryId, string direction)
        {
            var result = await _linksService.MoveCategoryAsync(categoryId, direction);

            if (!result.Success)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(ManageCategories));
        }

        // POST: /Links/UpdateLinkSortOrder - Admin only
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateLinkSortOrder(int linkId, string direction)
        {
            var result = await _linksService.MoveLinkAsync(linkId, direction);

            if (!result.Success)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(ManageLinks), new { categoryId = result.CategoryId });
        }

        // POST: /Links/DeleteCategory - Admin only
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int categoryId)
        {
            var result = await _linksService.DeleteCategoryAsync(categoryId);

            if (!result.Success)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(ManageCategories));
        }

        // POST: /Links/DeleteLink - Admin only
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLink(int linkId)
        {
            var result = await _linksService.DeleteLinkAsync(linkId);

            if (!result.Success)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(ManageLinks), new { categoryId = result.CategoryId });
        }

        // POST: /Links/UpdateLink - Admin only
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateLink(int linkId, string linkName, string linkUrl)
        {
            var result = await _linksService.UpdateLinkAsync(new UpdateLinkModel
            {
                LinkId = linkId,
                LinkName = linkName,
                LinkUrl = linkUrl
            });

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(ManageLinks), new { categoryId = result.CategoryId });
        }

        // POST: /Links/UpdateCategoriesSortOrder - Batch update for drag-and-drop
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCategoriesSortOrder(int[] categoryIds, int[] sortOrders)
        {
            var result = await _linksService.UpdateCategorySortOrdersAsync(categoryIds, sortOrders);
            return Json(new { success = result.Success, message = result.Message });
        }

        // POST: /Links/UpdateLinksSortOrder - Batch update for drag-and-drop
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateLinksSortOrder(int[] linkIds, int[] sortOrders)
        {
            var result = await _linksService.UpdateLinkSortOrdersAsync(linkIds, sortOrders);
            return Json(new { success = result.Success, message = result.Message });
        }
    }
}
