using System.Security.Claims;
using Ape.Models;
using Ape.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ape.Controllers;

public class StoreController(
    IProductCatalogService catalogService,
    ISubscriptionService subscriptionService,
    ILogger<StoreController> logger) : Controller
{
    private readonly IProductCatalogService _catalogService = catalogService;
    private readonly ISubscriptionService _subscriptionService = subscriptionService;
    private readonly ILogger<StoreController> _logger = logger;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var isMember = User.Identity?.IsAuthenticated == true && await HasActiveSubscriptionAsync();
        var viewModel = await _catalogService.GetStoreHomeAsync(isMember);

        ViewData["Title"] = "Shop";
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Browse(string? category, ProductType? type, string? search, string sort = "name", int page = 1)
    {
        int? categoryId = null;

        if (!string.IsNullOrEmpty(category))
        {
            var cat = await _catalogService.GetCategoryBySlugAsync(category);
            if (cat != null)
                categoryId = cat.CategoryId;
        }

        var isMember = User.Identity?.IsAuthenticated == true && await HasActiveSubscriptionAsync();
        var viewModel = await _catalogService.BuildBrowseViewModelAsync(categoryId, type, search, sort, page, 24, isMember);

        ViewData["Title"] = viewModel.CurrentCategoryName;
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Product(string slug)
    {
        if (string.IsNullOrEmpty(slug))
            return RedirectToAction(nameof(Browse));

        var isMember = User.Identity?.IsAuthenticated == true && await HasActiveSubscriptionAsync();
        var viewModel = await _catalogService.GetProductDetailAsync(slug, isMember);

        if (viewModel == null)
        {
            TempData["ErrorMessage"] = "Product not found.";
            return RedirectToAction(nameof(Browse));
        }

        ViewData["Title"] = viewModel.Product.Name;
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Search(string q, ProductType? type, string sort = "name", int page = 1)
    {
        if (string.IsNullOrWhiteSpace(q))
            return RedirectToAction(nameof(Browse));

        var isMember = User.Identity?.IsAuthenticated == true && await HasActiveSubscriptionAsync();
        var viewModel = await _catalogService.BuildBrowseViewModelAsync(null, type, q, sort, page, 24, isMember);

        ViewData["Title"] = $"Search: {q}";
        return View("Browse", viewModel);
    }

    private async Task<bool> HasActiveSubscriptionAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return false;
        return await _subscriptionService.HasActiveSubscriptionAsync(userId);
    }
}
