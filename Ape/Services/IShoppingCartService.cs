using Ape.Models.ViewModels;

namespace Ape.Services;

public interface IShoppingCartService
{
    Task<CartViewModel> GetCartAsync(string userId, bool isMember = false);
    Task<StoreOperationResult> AddItemAsync(string userId, int productId, int quantity, bool isMember);
    Task<StoreOperationResult> UpdateQuantityAsync(string userId, int cartItemId, int quantity);
    Task<StoreOperationResult> RemoveItemAsync(string userId, int cartItemId);
    Task ClearCartAsync(string userId);
    Task<int> GetCartItemCountAsync(string userId);
}
