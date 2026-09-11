
using SupplierInventoryService.Data;
using SupplierInventoryService.DTOs;
using SupplierInventoryService.Repositories.Interfaces;
using SupplierInventoryService.Entities;
using SupplierInventoryService.Services.Interfaces;
using SupplierInventoryService.ExceptionMiddleware;
namespace SupplierInventoryService.Services.Implementations;

public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _supplierRepository;

    public SupplierService(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task<SupplierResponse> CreateSupplierAsync(CreateSupplierRequest request)
    {
        var supplier = new Supplier
        {
            Name = request.Name,
            ContactInfo = request.ContactInfo,
            Address = request.Address,
            IsActive = true
        };

        await _supplierRepository.AddAsync(supplier);
        await _supplierRepository.SaveChangesAsync();

        return ToResponse(supplier);
    }

    public async Task<IEnumerable<SupplierResponse>> GetAllSuppliersAsync()
    {
        var suppliers = await _supplierRepository.GetAllAsync();
        return suppliers.Select(ToResponse);
    }

    public async Task<SupplierResponse> GetSupplierByIdAsync(int id)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Supplier not found.");
        return ToResponse(supplier);
    }

    public async Task<SupplierResponse> UpdateSupplierAsync(int id, UpdateSupplierRequest request)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Supplier not found.");

        supplier.Name = request.Name;
        supplier.ContactInfo = request.ContactInfo;
        supplier.Address = request.Address;
        supplier.IsActive = request.IsActive;

        await _supplierRepository.SaveChangesAsync();
        return ToResponse(supplier);
    }

    public async Task DeactivateSupplierAsync(int id)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Supplier not found.");

        supplier.IsActive = false;
        await _supplierRepository.SaveChangesAsync();
    }

    private static SupplierResponse ToResponse(Supplier s) =>
        new(s.Id, s.Name, s.ContactInfo, s.Address, s.IsActive);
}