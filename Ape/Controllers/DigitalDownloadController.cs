using System.Security.Claims;
using Ape.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ape.Controllers;

[Authorize]
public class DigitalDownloadController(
    IDigitalDeliveryService deliveryService,
    ILogger<DigitalDownloadController> logger) : Controller
{
    private readonly IDigitalDeliveryService _deliveryService = deliveryService;
    private readonly ILogger<DigitalDownloadController> _logger = logger;

    [HttpGet("download/{token}")]
    public async Task<IActionResult> Download(string token)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var result = await _deliveryService.ServeFileAsync(token, userId);
        if (result == null)
        {
            TempData["ErrorMessage"] = "Download link is invalid, expired, or you have reached the maximum downloads.";
            return RedirectToAction("Index", "OrderHistory");
        }

        var (fileStream, contentType, fileName) = result.Value;
        if (fileStream == null)
        {
            TempData["ErrorMessage"] = "File not found.";
            return RedirectToAction("Index", "OrderHistory");
        }

        return File(fileStream, contentType!, fileName);
    }
}
