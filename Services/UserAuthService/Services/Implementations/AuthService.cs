using UserAuthService.Services.Interfaces;
using UserAuthService.DTOs;
using UserAuthService.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using UserAuthService.Entities;
using UserAuthService.ExceptionMiddleware;
using Azure;
using Org.BouncyCastle.Bcpg;
namespace UserAuthService.Services.Implementations;

public class AuthService : IAuthService
{

    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthService(IUserRepository userRepository,IRoleRepository roleRepository,IRefreshTokenRepository refreshTokenRepository,
    IPasswordResetTokenRepository resetTokenRepository,ITokenService tokenService,IEmailService emailService,IPasswordHasher<User> passwordHasher)
    {
        _userRepository=userRepository;
        _roleRepository=roleRepository;
        _refreshTokenRepository=refreshTokenRepository;
        _emailService=emailService;
        _passwordResetTokenRepository=resetTokenRepository;
        _passwordHasher=passwordHasher;
        _tokenService=tokenService;
    }
    public async Task<SignUpResponse> SignUpAsync(SignUpRequest request)
    {
        var exists= await _userRepository.GetByEmailAsync(request.Email);
        if (exists is not null)
        {
            throw new ConflictException("Email already exists");
        }
        var doctorRole=await _roleRepository.GetByNameAsync("DOCTOR");
        if(doctorRole is null) throw new InvalidOperationException("DOCTOR role not seeded");

        User user=new User
        {
            Name=request.Name,
            Email=request.Email,
            RoleId=doctorRole.Id,
            CreatedAt=DateTime.UtcNow

        };
        user.PasswordHash=_passwordHasher.HashPassword(user,request.Password);
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();
        await _emailService.SendSignUpConfirmationAsync(user.Email,user.Name);
        return new SignUpResponse(user.Id,user.Name,user.Email);
    }
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var exists=await _userRepository.GetByEmailAsync(request.Email) ??
            throw new NotFoundException("User is not signed up");
        var pass=_passwordHasher.VerifyHashedPassword(exists,exists.PasswordHash,request.Password);
        if (pass == PasswordVerificationResult.Failed)
        {
            throw new InvalidCredentialsException();
        }
        var accessToken=_tokenService.GenerateAccessToken(exists);
        var refreshToken=_tokenService.GenerateRefreshToken();
        await _refreshTokenRepository.AddAsync(new RefreshToken
        {
            UserId = exists.Id,
            TokenHash = _tokenService.HashToken(refreshToken),
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await _refreshTokenRepository.SaveChangesAsync();
        return new LoginResponse(accessToken,refreshToken);
    }

    public async Task LogoutAsync(string rawRefreshToken)
    {
        var hash = _tokenService.HashToken(rawRefreshToken);
        var token = await _refreshTokenRepository.GetByTokenHashAsync(hash)
            ?? throw new NotFoundException("Refresh token not found.");

        token.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.SaveChangesAsync();
    }

    public async Task<RefreshResponse> RefreshAsync(string rawRefreshToken)
    {
        var hash = _tokenService.HashToken(rawRefreshToken);
        var token = await _refreshTokenRepository.GetByTokenHashAsync(hash)
            ?? throw new InvalidCredentialsException();

        if (token.RevokedAt is not null)
            throw new InvalidCredentialsException(); // reused/revoked token — treat as compromised
        if (token.ExpiresAt < DateTime.UtcNow)
            throw new TokenExpiredException("token is expired");

        token.RevokedAt=DateTime.UtcNow;

        var accessToken=_tokenService.GenerateAccessToken(token.User);
        var refreshToken=_tokenService.GenerateRefreshToken();
        await _refreshTokenRepository.AddAsync(new RefreshToken
        {
            UserId = token.User.Id,
            TokenHash = _tokenService.HashToken(refreshToken),
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await _refreshTokenRepository.SaveChangesAsync();

        return new RefreshResponse(accessToken, refreshToken);
    }

    public async Task ForgetPasswordAsync(string email)
    {
        var user=await _userRepository.GetByEmailAsync(email);
        if(user is null) return;
        var refresh=_tokenService.GenerateRefreshToken();
        await _passwordResetTokenRepository.AddAsync(new PasswordResetToken
        {
            UserId=user.Id,
            TokenHash=_tokenService.HashToken(refresh),
            ExpiresAt=DateTime.UtcNow.AddMinutes(5)
        });
        await _passwordResetTokenRepository.SaveChangesAsync();

        await _emailService.SendPasswordResetAsync(user.Email,refresh);
    }

    public async Task ResetPasswordAsync(string rawRefreshToken,string newPassword)
    {
        var hash=_tokenService.HashToken(rawRefreshToken);
        var reset=await _passwordResetTokenRepository.GetByTokenHashAsync(hash) ?? 
        throw new NotFoundException("reset token not found");

        if (reset.UsedAt is not null)
            throw new TokenAlreadyUsedException("reset token is already used");
        if (reset.ExpiresAt < DateTime.UtcNow)
            throw new TokenExpiredException("reset token expired");

        reset.User.PasswordHash=_passwordHasher.HashPassword(reset.User,newPassword);
        reset.UsedAt=DateTime.UtcNow;
        await _passwordResetTokenRepository.SaveChangesAsync();

    }
}