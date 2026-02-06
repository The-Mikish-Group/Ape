using Ape.Models.ViewModels;

namespace Ape.Services;

public interface IShippingAddressService
{
    Task<List<ShippingAddressViewModel>> GetAddressesAsync(string userId);
    Task<ShippingAddressViewModel?> GetAddressByIdAsync(string userId, int addressId);
    Task<ShippingAddressViewModel?> GetDefaultAddressAsync(string userId);
    Task<StoreOperationResult> CreateAddressAsync(string userId, CreateShippingAddressModel model);
    Task<StoreOperationResult> UpdateAddressAsync(string userId, EditShippingAddressModel model);
    Task<StoreOperationResult> DeleteAddressAsync(string userId, int addressId);
    Task<StoreOperationResult> SetDefaultAddressAsync(string userId, int addressId);
}
