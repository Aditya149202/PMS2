using System.ComponentModel.DataAnnotations;
namespace SupplierInventoryService.DTOs;

public record SupplierResponse(int Id, string Name,[Phone] string ContactInfo, string Address,bool IsActive);
