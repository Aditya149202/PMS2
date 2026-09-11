using UserAuthService.DTOs;
namespace UserAuthService.Services.Interfaces;
public interface IEmailService
{ 
    Task SendSignUpConfirmationAsync(string toEmail,string userName);
    Task SendPasswordResetAsync(string toEmail,string rawResetToken);
}