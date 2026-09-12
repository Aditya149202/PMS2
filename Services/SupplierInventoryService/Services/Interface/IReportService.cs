using SupplierInventoryService.DTOs;

namespace SupplierInventoryService.Services.Interfaces;

public interface IReportService
{
    Task<SalesReportResponse> GetSalesReportAsync(DateTime? From,DateTime? To);
}