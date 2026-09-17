using System.ComponentModel.DataAnnotations;
namespace UserAuthService.DTOs;
public record SignUpRequest([Required]string Name,[Required,EmailAddress]string Email,[Required,MinLength(8)]string Password);