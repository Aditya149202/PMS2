using Microsoft.AspNetCore.Mvc;
using UserAuthService.DTOs;
using UserAuthService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
namespace UserAuthService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("signup")]
    // public async Task<ActionResult<SignUpResponse>> SignUp(SignUpRequest request)
    // {
    //     var response = await _authService.SignUpAsync(request);
    //     return Created(string.Empty, response);
    // }
    public async Task<IActionResult> Signup([FromBody] SignUpRequest request)
    {
        var result = await _authService.SignUpAsync(request);
        return CreatedAtAction(nameof(Signup), result);
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> LogOut([FromBody] LogOutRequest request)
    {
        await _authService.LogoutAsync(request.RefreshToken);
        return NoContent();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var result = await _authService.RefreshAsync(request.RefreshToken);
        return Ok(result);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgetPassword([FromBody] ForgetPasswordRequest request)
    {
        await _authService.ForgetPasswordAsync(request.Email);
        return NoContent(); // always 204 regardless of whether email exists — anti-enumeration
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await _authService.ResetPasswordAsync(request.Token, request.NewPassword);
        return NoContent();
    }
}





    

    
    

    
