using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace UserAuthService.DTOs;

public record LoginRequest([EmailAddress]string Email,[PasswordPropertyText]string Password);