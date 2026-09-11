namespace SupplierInventoryService.DTOs;
public record ReserveStockRequest(
    int PaymentIntentId,
    List<ReserveStockRequestItem> Items);