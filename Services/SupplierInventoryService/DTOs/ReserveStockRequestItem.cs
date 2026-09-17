using System.ComponentModel.DataAnnotations;
namespace SupplierInventoryService.DTOs;

public record ReserveStockRequestItem(int DrugId,[Range(1,int.MaxValue)] int Quantity);