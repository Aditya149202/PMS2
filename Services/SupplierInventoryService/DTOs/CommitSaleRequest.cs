using System.ComponentModel.DataAnnotations;
namespace SupplierInventoryService.DTOs;

public record CommitSaleRequest(
    int OrderId,
    int PaymentIntentId,
    [Range(0.01,double.MaxValue)]decimal Amount
);