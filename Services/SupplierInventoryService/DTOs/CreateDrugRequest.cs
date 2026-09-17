using System.ComponentModel.DataAnnotations;
namespace SupplierInventoryService.DTOs;


public record CreateDrugRequest([Required,StringLength(200,MinimumLength =1)]string Name,[Range(0.01,double.MaxValue)] decimal Price,[Range(0,int.MaxValue)] int QuantityInStock, int SupplierId);