using System.Security.Claims;
using Ape.Models.ViewModels;
using Ape.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ape.Controllers;

[Authorize]
public class OrderHistoryController(
    IShippingAddressService addressService,
    IOrderService orderService,
    IDigitalDeliveryService deliveryService,
    ILogger<OrderHistoryController> logger) : Controller
{
    private readonly IShippingAddressService _addressService = addressService;
    private readonly IOrderService _orderService = orderService;
    private readonly IDigitalDeliveryService _deliveryService = deliveryService;
    private readonly ILogger<OrderHistoryController> _logger = logger;

    // ============================================================
    // Orders
    // ============================================================

    public async Task<IActionResult> Index(int page = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var orders = await _orderService.GetUserOrdersAsync(userId, page);

        ViewData["Title"] = "My Orders";
        ViewData["CurrentPage"] = page;
        return View(orders);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var order = await _orderService.GetOrderDetailAsync(id);

        if (order == null)
        {
            TempData["ErrorMessage"] = "Order not found.";
            return RedirectToAction(nameof(Index));
        }

        if (order.HasDigitalItems)
        {
            order.Downloads = await _deliveryService.GetDownloadsForOrderAsync(id, userId);
        }

        ViewData["Title"] = $"Order {order.OrderNumber}";
        return View(order);
    }

    public async Task<IActionResult> Downloads()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var downloads = await _deliveryService.GetAllUserDownloadsAsync(userId);

        ViewData["Title"] = "My Downloads";
        return View(downloads);
    }

    // ============================================================
    // Addresses
    // ============================================================

    public async Task<IActionResult> Addresses()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var addresses = await _addressService.GetAddressesAsync(userId);

        ViewData["Title"] = "My Addresses";
        return View(addresses);
    }

    public IActionResult AddAddress()
    {
        ViewData["Title"] = "Add Address";
        return View(new CreateShippingAddressModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAddress(CreateShippingAddressModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _addressService.CreateAddressAsync(userId, model);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Addresses));
    }

    public async Task<IActionResult> EditAddress(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var address = await _addressService.GetAddressByIdAsync(userId, id);

        if (address == null)
        {
            TempData["ErrorMessage"] = "Address not found.";
            return RedirectToAction(nameof(Addresses));
        }

        var model = new EditShippingAddressModel
        {
            AddressId = address.AddressId,
            FullName = address.FullName,
            AddressLine1 = address.AddressLine1,
            AddressLine2 = address.AddressLine2,
            City = address.City,
            State = address.State,
            ZipCode = address.ZipCode,
            Country = address.Country,
            Phone = address.Phone,
            IsDefault = address.IsDefault
        };

        ViewData["Title"] = "Edit Address";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAddress(EditShippingAddressModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _addressService.UpdateAddressAsync(userId, model);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAddress(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _addressService.DeleteAddressAsync(userId, id);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefaultAddress(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _addressService.SetDefaultAddressAsync(userId, id);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Addresses));
    }
}
