using Ape.Data;
using Ape.Models;
using Ape.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Ape.Services;

public class ShippingAddressService(
    ApplicationDbContext context,
    ILogger<ShippingAddressService> logger) : IShippingAddressService
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<ShippingAddressService> _logger = logger;

    public async Task<List<ShippingAddressViewModel>> GetAddressesAsync(string userId)
    {
        return await _context.ShippingAddresses
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedDate)
            .Select(a => MapToViewModel(a))
            .ToListAsync();
    }

    public async Task<ShippingAddressViewModel?> GetAddressByIdAsync(string userId, int addressId)
    {
        return await _context.ShippingAddresses
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.AddressID == addressId)
            .Select(a => MapToViewModel(a))
            .FirstOrDefaultAsync();
    }

    public async Task<ShippingAddressViewModel?> GetDefaultAddressAsync(string userId)
    {
        return await _context.ShippingAddresses
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.IsDefault)
            .Select(a => MapToViewModel(a))
            .FirstOrDefaultAsync();
    }

    public async Task<StoreOperationResult> CreateAddressAsync(string userId, CreateShippingAddressModel model)
    {
        if (string.IsNullOrWhiteSpace(model.FullName) || string.IsNullOrWhiteSpace(model.AddressLine1) ||
            string.IsNullOrWhiteSpace(model.City) || string.IsNullOrWhiteSpace(model.ZipCode))
            return StoreOperationResult.Failed("Please fill in all required address fields.");

        // If this is the default or first address, clear other defaults
        if (model.IsDefault || !await _context.ShippingAddresses.AnyAsync(a => a.UserId == userId))
        {
            await ClearDefaultAddressAsync(userId);
            model.IsDefault = true;
        }

        var address = new ShippingAddress
        {
            UserId = userId,
            FullName = model.FullName.Trim(),
            AddressLine1 = model.AddressLine1.Trim(),
            AddressLine2 = model.AddressLine2?.Trim(),
            City = model.City.Trim(),
            State = model.State?.Trim(),
            ZipCode = model.ZipCode.Trim(),
            Country = model.Country,
            Phone = model.Phone?.Trim(),
            IsDefault = model.IsDefault,
            CreatedDate = DateTime.UtcNow
        };

        _context.ShippingAddresses.Add(address);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created shipping address for user {UserId} (ID: {AddressId})", userId, address.AddressID);
        return StoreOperationResult.Succeeded(address.AddressID, "Address saved.");
    }

    public async Task<StoreOperationResult> UpdateAddressAsync(string userId, EditShippingAddressModel model)
    {
        var address = await _context.ShippingAddresses.FirstOrDefaultAsync(a => a.UserId == userId && a.AddressID == model.AddressId);
        if (address == null)
            return StoreOperationResult.Failed("Address not found.");

        if (model.IsDefault)
            await ClearDefaultAddressAsync(userId);

        address.FullName = model.FullName.Trim();
        address.AddressLine1 = model.AddressLine1.Trim();
        address.AddressLine2 = model.AddressLine2?.Trim();
        address.City = model.City.Trim();
        address.State = model.State?.Trim();
        address.ZipCode = model.ZipCode.Trim();
        address.Country = model.Country;
        address.Phone = model.Phone?.Trim();
        address.IsDefault = model.IsDefault;

        await _context.SaveChangesAsync();

        return StoreOperationResult.Succeeded(address.AddressID, "Address updated.");
    }

    public async Task<StoreOperationResult> DeleteAddressAsync(string userId, int addressId)
    {
        var address = await _context.ShippingAddresses.FirstOrDefaultAsync(a => a.UserId == userId && a.AddressID == addressId);
        if (address == null)
            return StoreOperationResult.Failed("Address not found.");

        _context.ShippingAddresses.Remove(address);
        await _context.SaveChangesAsync();

        return StoreOperationResult.SucceededNoId("Address deleted.");
    }

    public async Task<StoreOperationResult> SetDefaultAddressAsync(string userId, int addressId)
    {
        await ClearDefaultAddressAsync(userId);

        var address = await _context.ShippingAddresses.FirstOrDefaultAsync(a => a.UserId == userId && a.AddressID == addressId);
        if (address == null)
            return StoreOperationResult.Failed("Address not found.");

        address.IsDefault = true;
        await _context.SaveChangesAsync();

        return StoreOperationResult.SucceededNoId("Default address updated.");
    }

    private async Task ClearDefaultAddressAsync(string userId)
    {
        await _context.ShippingAddresses
            .Where(a => a.UserId == userId && a.IsDefault)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false));
    }

    private static ShippingAddressViewModel MapToViewModel(ShippingAddress a) => new()
    {
        AddressId = a.AddressID,
        FullName = a.FullName,
        AddressLine1 = a.AddressLine1,
        AddressLine2 = a.AddressLine2,
        City = a.City,
        State = a.State,
        ZipCode = a.ZipCode,
        Country = a.Country,
        Phone = a.Phone,
        IsDefault = a.IsDefault
    };
}
