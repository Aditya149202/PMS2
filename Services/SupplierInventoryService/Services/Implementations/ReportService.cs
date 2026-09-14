using SupplierInventoryService.DTOs;
using SupplierInventoryService .Repositories.Interfaces;
using SupplierInventoryService.Services.Interfaces;

namespace SupplierInventoryService.Services.Implementations;

public class ReportService : IReportService
{
    private readonly ISalesRepository _salesRepository;
    public ReportService(ISalesRepository salesRepository)
    {
        _salesRepository=salesRepository;
    }

    public async Task<SalesReportResponse> GetSalesReportAsync(DateTime? from,DateTime? to)
    {
        var sales=await _salesRepository.GetByDateRangeAsync(from,to);
        return new SalesReportResponse(from,to,sales.Count,sales.Sum(s => s.Amount));
    }
}