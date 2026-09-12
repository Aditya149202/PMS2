using OrderService.DTOs;
namespace OrderService.Services;

public interface IPaymentService
{
    Task<PaymentInitiateResponse> InitiatePaymentAsync(int doctorId, string doctorName, PaymentInitiateRequest request);
    Task<PaymentStatusResponse> ConfirmPaymentAsync(int doctorId, string doctorName, PaymentConfirmRequest request);
    Task<PaymentStatusResponse> GetPaymentStatusAsync(int paymentIntentId, int? requestingDoctorId);

    Task ExpiredPaymentIntentAsync(int paymentIntentId);
    //bool VerifyPaymentSignature(string razorpayOrderId, string razorpayPaymentId, string signature);
}