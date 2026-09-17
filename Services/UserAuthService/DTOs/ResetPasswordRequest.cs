using System.ComponentModel.DataAnnotations;

namespace UserAuthService.DTOs;

public record ResetPasswordRequest([Required]string Token,[Required,MinLength(8)]string NewPassword);