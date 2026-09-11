using SupplierInventoryService.Data;
using SupplierInventoryService.DTOs;
using SupplierInventoryService.Repositories.Interfaces;
using SupplierInventoryService.Entities;
using SupplierInventoryService.Services.Interfaces;
using SupplierInventoryService.Repositories.Interfaces;
using SupplierInventoryService.ExceptionMiddleware;
namespace SupplierInventoryService.Services.Implementations;

public class DrugService : IDrugService
{
    private readonly IDrugRepository _drugRepository;
    private readonly ISupplierRepository _supplierRepository;

    public DrugService(IDrugRepository drugRepository, ISupplierRepository supplierRepository)
    {
        _drugRepository = drugRepository;
        _supplierRepository = supplierRepository;
    }

    public async Task<DrugResponse> CreateDrugAsync(CreateDrugRequest request)
    {
        // Validate the FK up front for a clean NotFoundException,
        // rather than letting SQL throw a raw FK-violation exception.
        _ = await _supplierRepository.GetByIdAsync(request.SupplierId)
            ?? throw new NotFoundException("Supplier not found.");

        var drug = new Drug
        {
            Name = request.Name,
            Price = request.Price,
            QuantityInStock = request.QuantityInStock,
            SupplierId = request.SupplierId,
            IsActive = true
        };

        await _drugRepository.AddAsync(drug);
        await _drugRepository.SaveChangesAsync();

        return ToResponse(drug);
    }

    public async Task<IEnumerable<DrugResponse>> GetAllDrugsAsync(bool includeInactive)
    {
        var drugs = await _drugRepository.GetAllAsync();
        if(!includeInactive)
        {
            drugs = drugs.Where(d => d.IsActive);
        }
        return drugs.Select(ToResponse);
    }

    public async Task<DrugResponse> GetDrugByIdAsync(int id,bool includeInactive)
    {
        var drug = await _drugRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Drug not found.");
        if(!includeInactive && !drug.IsActive)
        {
            throw new NotFoundException("Drug not found.");
        }
        return ToResponse(drug);
    }

    public async Task<DrugResponse> UpdateDrugAsync(int id, UpdateDrugRequest request)
    {
        var drug = await _drugRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Drug not found.");

        drug.Name = request.Name;
        drug.Price = request.Price;
        drug.QuantityInStock = request.QuantityInStock;
        drug.IsActive = request.IsActive;

        await _drugRepository.SaveChangesAsync();
        return ToResponse(drug);
    }

    public async Task DeactivateDrugAsync(int id)
    {
        var drug = await _drugRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Drug not found.");

        drug.IsActive = false;
        await _drugRepository.SaveChangesAsync();
    }

    private static DrugResponse ToResponse(Drug d) =>
        new(d.Id, d.Name, d.Price, d.QuantityInStock, d.IsActive, d.SupplierId);
}