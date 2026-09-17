using System.ComponentModel.DataAnnotations;
namespace SupplierInventoryService.DTOs;

public record ReserveStockResponseItem(
    int DrugId,
    string DrugName,
    decimal UnitPrice);