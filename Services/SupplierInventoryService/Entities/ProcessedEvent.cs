namespace SupplierInventoryService.Entities;

public class ProcessedEvent
{
    public int OrderId { get; set; }
    
    public DateTime ProcessedAt { get; set; }
}