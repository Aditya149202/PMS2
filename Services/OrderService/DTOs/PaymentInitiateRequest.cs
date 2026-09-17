using System.ComponentModel.DataAnnotations;
namespace OrderService.DTOs;

public record PaymentInitiateRequestItem(int DrugId, [Range(1,int.MaxValue)]int Quantity);

public record PaymentInitiateRequest(List<PaymentInitiateRequestItem> Items);