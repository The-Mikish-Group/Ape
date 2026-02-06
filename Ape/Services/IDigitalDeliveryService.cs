using Ape.Models.ViewModels;

namespace Ape.Services;

public interface IDigitalDeliveryService
{
    Task CreateDownloadRecordsForOrderAsync(int orderId);
    Task<List<DownloadLinkViewModel>> GetDownloadsForOrderAsync(int orderId, string userId);
    Task<List<DownloadLinkViewModel>> GetAllUserDownloadsAsync(string userId);
    Task<(Stream? FileStream, string? ContentType, string? FileName)?> ServeFileAsync(string downloadToken, string userId);
}
