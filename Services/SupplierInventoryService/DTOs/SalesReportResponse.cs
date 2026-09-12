namespace SupplierInventoryService.DTOs;

public record SalesReportResponse
(
     DateTime? DateFrom,
     DateTime? DateTo,
     int TotalOrders,
     decimal TotalAmount

);