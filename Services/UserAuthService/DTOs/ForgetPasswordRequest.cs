using System.ComponentModel.DataAnnotations;
namespace UserAuthService.DTOs;

public record ForgetPasswordRequest([Required]string Email);