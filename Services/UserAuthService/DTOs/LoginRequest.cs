using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace UserAuthService.DTOs;

public record LoginRequest([EmailAddress,Required]string Email,[Required]string Password);