namespace UserAuthService.DTOs;

public record ResetPasswordRequest(string Token,string NewPassword);