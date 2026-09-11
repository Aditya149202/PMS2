namespace SupplierInventoryService.DTOs;

public record CommitSaleRequest(
    int OrderId,
    int PaymentIntentId,
    decimal Amount
);