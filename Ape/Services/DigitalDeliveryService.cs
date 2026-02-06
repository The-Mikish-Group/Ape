using System.Security.Cryptography;
using Ape.Data;
using Ape.Models;
using Ape.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Ape.Services;

public class DigitalDeliveryService(
    ApplicationDbContext context,
    IWebHostEnvironment environment,
    ILogger<DigitalDeliveryService> logger) : IDigitalDeliveryService
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<DigitalDeliveryService> _logger = logger;
    private readonly string _digitalFilesPath = Path.Combine(environment.ContentRootPath, "ProtectedFiles", "store");

    public async Task CreateDownloadRecordsForOrderAsync(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderID == orderId);

        if (order == null) return;

        var digitalItems = order.Items.Where(i => i.ProductType == ProductType.Digital).ToList();

        foreach (var item in digitalItems)
        {
            var digitalFiles = await _context.DigitalProductFiles
                .Where(f => f.ProductID == item.ProductID)
                .ToListAsync();

            var product = await _context.Products.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductID == item.ProductID);

            foreach (var file in digitalFiles)
            {
                var token = GenerateDownloadToken();
                var download = new CustomerDownload
                {
                    OrderItemID = item.OrderItemID,
                    UserId = order.UserId,
                    ProductID = item.ProductID,
                    DigitalFileID = file.FileID,
                    DownloadToken = token,
                    DownloadCount = 0,
                    MaxDownloads = product?.MaxDownloads ?? 5,
                    ExpiresDate = DateTime.UtcNow.AddDays(product?.DownloadExpiryDays ?? 30),
                    CreatedDate = DateTime.UtcNow
                };

                _context.CustomerDownloads.Add(download);
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Created download records for order {OrderId}", orderId);
    }

    public async Task<List<DownloadLinkViewModel>> GetDownloadsForOrderAsync(int orderId, string userId)
    {
        return await _context.CustomerDownloads
            .AsNoTracking()
            .Include(d => d.DigitalFile)
            .Include(d => d.Product)
            .Where(d => d.OrderItem!.OrderID == orderId && d.UserId == userId)
            .Select(d => new DownloadLinkViewModel
            {
                ProductName = d.Product!.Name,
                OriginalFileName = d.DigitalFile!.OriginalFileName,
                DownloadToken = d.DownloadToken,
                DownloadCount = d.DownloadCount,
                MaxDownloads = d.MaxDownloads,
                ExpiresDate = d.ExpiresDate
            })
            .ToListAsync();
    }

    public async Task<List<DownloadLinkViewModel>> GetAllUserDownloadsAsync(string userId)
    {
        return await _context.CustomerDownloads
            .AsNoTracking()
            .Include(d => d.DigitalFile)
            .Include(d => d.Product)
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.CreatedDate)
            .Select(d => new DownloadLinkViewModel
            {
                ProductName = d.Product!.Name,
                OriginalFileName = d.DigitalFile!.OriginalFileName,
                DownloadToken = d.DownloadToken,
                DownloadCount = d.DownloadCount,
                MaxDownloads = d.MaxDownloads,
                ExpiresDate = d.ExpiresDate
            })
            .ToListAsync();
    }

    public async Task<(Stream? FileStream, string? ContentType, string? FileName)?> ServeFileAsync(string downloadToken, string userId)
    {
        var download = await _context.CustomerDownloads
            .Include(d => d.DigitalFile)
            .FirstOrDefaultAsync(d => d.DownloadToken == downloadToken && d.UserId == userId);

        if (download == null)
        {
            _logger.LogWarning("Download attempt with invalid token {Token} by user {UserId}", downloadToken, userId);
            return null;
        }

        if (download.ExpiresDate.HasValue && download.ExpiresDate < DateTime.UtcNow)
        {
            _logger.LogWarning("Download attempt with expired token {Token}", downloadToken);
            return null;
        }

        if (download.DownloadCount >= download.MaxDownloads)
        {
            _logger.LogWarning("Download attempt exceeding max downloads for token {Token}", downloadToken);
            return null;
        }

        var file = download.DigitalFile;
        if (file == null) return null;

        var filePath = Path.Combine(_digitalFilesPath, file.FileName);
        if (!File.Exists(filePath))
        {
            _logger.LogError("Digital file not found on disk: {FilePath}", filePath);
            return null;
        }

        // Update download count
        download.DownloadCount++;
        download.LastDownloadDate = DateTime.UtcNow;
        if (download.FirstDownloadDate == null)
            download.FirstDownloadDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return (stream, file.ContentType ?? "application/octet-stream", file.OriginalFileName ?? file.FileName);
    }

    private static string GenerateDownloadToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
