using System.ComponentModel.DataAnnotations;
namespace SupplierInventoryService.DTOs;

public record SupplierResponse(int Id,[Required] string Name, string ContactInfo, string Address,bool IsActive);
