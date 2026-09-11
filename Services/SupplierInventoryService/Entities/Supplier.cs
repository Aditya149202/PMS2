namespace SupplierInventoryService.Entities;

public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string ContactInfo { get; set; } = default!;
    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;
    public ICollection<Drug> Drugs { get; set; } = new List<Drug>();
}