using UserAuthService.DTOs;
namespace UserAuthService.Services.Interfaces;
public interface IAuthService
{
    Task<SignUpResponse> SignUpAsync(SignUpRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);

    Task LogoutAsync(string rawRefereshToken);

    Task<RefreshResponse> RefreshAsync(string rawRefereshToken);

    Task ForgetPasswordAsync(string email);

    Task ResetPasswordAsync(string rawRefereshToken,string newPassword);
}