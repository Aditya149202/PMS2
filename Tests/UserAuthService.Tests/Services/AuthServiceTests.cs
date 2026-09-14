using Moq;
using NUnit.Framework;
using Microsoft.AspNetCore.Identity;

using UserAuthService.Services.Implementations;
using UserAuthService.Services.Interfaces;
using UserAuthService.Repositories.Interfaces;
using UserAuthService.Entities;
using UserAuthService.DTOs;
using UserAuthService.ExceptionMiddleware;


namespace UserAuthService.Tests.Services;

[TestFixture]
public class AuthServiceTests
{   
    #region Mocks
    private Mock<IUserRepository> _userRepo;
    private Mock<IRoleRepository> _roleRepo;
    private Mock<IRefreshTokenRepository> _refreshTokenRepo;
    private Mock<IPasswordResetTokenRepository> _passwordResetTokenRepo;
    private Mock<ITokenService> _tokenService;
    private Mock<IEmailService> _emailService;

    private PasswordHasher<User> _passwordHasher;

    private AuthService _authService;
    #endregion
    [SetUp]
    public void Setup()
    {
        _userRepo = new Mock<IUserRepository>();
        _roleRepo = new Mock<IRoleRepository>();
        _refreshTokenRepo = new Mock<IRefreshTokenRepository>();
        _passwordResetTokenRepo = new Mock<IPasswordResetTokenRepository>();
        _tokenService = new Mock<ITokenService>();
        _emailService = new Mock<IEmailService>();

        _passwordHasher = new PasswordHasher<User>();

        _authService = new AuthService(
            _userRepo.Object,
            _roleRepo.Object,
            _refreshTokenRepo.Object,
            _passwordResetTokenRepo.Object,
            _tokenService.Object,
            _emailService.Object,
            _passwordHasher);
    }


[Test]
public async Task SignUpAsync_Should_Create_User()
{
    var request = new SignUpRequest
    (
        "John",
        "john@test.com",
        "Password123!"
    );

    _userRepo.Setup(x => x.GetByEmailAsync(request.Email))
        .ReturnsAsync((User)null);

    _roleRepo.Setup(x => x.GetByNameAsync("DOCTOR"))
        .ReturnsAsync(new Role
        {
            Id = 1,
            Name = "DOCTOR"
        });

    var response = await _authService.SignUpAsync(request);

    Assert.That(response.Email, Is.EqualTo(request.Email));

    _userRepo.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Once);
    _userRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
    _emailService.Verify(x =>
        x.SendSignUpConfirmationAsync(request.Email, request.Name),
        Times.Once);
}
[Test]
public void SignUpAsync_Should_Throw_When_Email_Exists()
{
    var request = new SignUpRequest
    (
        "John",
        "john@test.com",
        "Password123!"
    );

    _userRepo.Setup(x => x.GetByEmailAsync(request.Email))
        .ReturnsAsync(new User());

    Assert.ThrowsAsync<ConflictException>(
        async () => await _authService.SignUpAsync(request));
}

[Test]
public void SignUpAsync_Should_Throw_When_Role_Missing()
{
    var request = new SignUpRequest(
        "John",
        "john@test.com",
        "Password123!");

    _userRepo.Setup(x => x.GetByEmailAsync(request.Email))
        .ReturnsAsync((User)null);

    _roleRepo.Setup(x => x.GetByNameAsync("DOCTOR"))
        .ReturnsAsync((Role)null);

    Assert.ThrowsAsync<InvalidOperationException>(
        async () => await _authService.SignUpAsync(request));
}

[Test]
public async Task LoginAsync_Should_Return_Tokens()
{
    var user = new User
    {
        Id = 1,
        Email = "john@test.com"
    };

    user.PasswordHash =
        _passwordHasher.HashPassword(user, "Password123");

    var request = new LoginRequest("john@test.com", "Password123");

    _userRepo.Setup(x => x.GetByEmailAsync(request.Email))
        .ReturnsAsync(user);

    _tokenService.Setup(x => x.GenerateAccessToken(user))
        .Returns("access-token");

    _tokenService.Setup(x => x.GenerateRefreshToken())
        .Returns("refresh-token");

    var result = await _authService.LoginAsync(request);

    Assert.That(result.AccessToken, Is.EqualTo("access-token"));
    Assert.That(result.RefreshToken, Is.EqualTo("refresh-token"));

    _refreshTokenRepo.Verify(x =>
        x.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
}



[Test]
public void LoginAsync_Should_Throw_When_Password_Is_Wrong()
{
    var user = new User
    {
        Email = "john@test.com"
    };

    user.PasswordHash =
        _passwordHasher.HashPassword(user, "ActualPassword");

    var request = new LoginRequest("john@test.com", "WrongPassword");

    _userRepo.Setup(x => x.GetByEmailAsync(request.Email))
        .ReturnsAsync(user);

    Assert.ThrowsAsync<InvalidCredentialsException>(
        async () => await _authService.LoginAsync(request));
}

[Test]
public async Task LogoutAsync_Should_Revoke_Token()
{
    var refreshToken = new RefreshToken();

    _tokenService.Setup(x => x.HashToken("raw-token"))
        .Returns("hashed");

    _refreshTokenRepo.Setup(x => x.GetByTokenHashAsync("hashed"))
        .ReturnsAsync(refreshToken);

    await _authService.LogoutAsync("raw-token");

    Assert.That(refreshToken.RevokedAt, Is.Not.Null);

    _refreshTokenRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
}

[Test]
public void RefreshAsync_Should_Throw_When_Expired()
{
    var token = new RefreshToken
    {
        ExpiresAt = DateTime.UtcNow.AddMinutes(-5)
    };

    _tokenService.Setup(x => x.HashToken("raw"))
        .Returns("hash");

    _refreshTokenRepo.Setup(x => x.GetByTokenHashAsync("hash"))
        .ReturnsAsync(token);

    Assert.ThrowsAsync<TokenExpiredException>(
        async () => await _authService.RefreshAsync("raw"));
}


[Test]
public void LoginAsync_Should_Throw_When_User_Not_Found()
{
    var request = new LoginRequest("missing@test.com", "Password123");

    _userRepo.Setup(x => x.GetByEmailAsync(request.Email))
        .ReturnsAsync((User)null);

    Assert.ThrowsAsync<InvalidCredentialsException>(
        async () => await _authService.LoginAsync(request));
}

[Test]
public async Task RefreshAsync_Should_Rotate_Token()
{
    var oldToken = new RefreshToken
    {
        UserId = 1,
        User = new User { Id = 1 },
        ExpiresAt = DateTime.UtcNow.AddDays(1),
        RevokedAt = null
    };

    _tokenService.Setup(x => x.HashToken("raw")).Returns("hash");
    _refreshTokenRepo.Setup(x => x.GetByTokenHashAsync("hash")).ReturnsAsync(oldToken);
    _tokenService.Setup(x => x.GenerateAccessToken(oldToken.User)).Returns("new-access");
    _tokenService.Setup(x => x.GenerateRefreshToken()).Returns("new-refresh");

    var result = await _authService.RefreshAsync("raw");

    Assert.That(oldToken.RevokedAt, Is.Not.Null);
    _refreshTokenRepo.Verify(x => x.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
    _refreshTokenRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
    Assert.That(result.AccessToken, Is.EqualTo("new-access"));
    Assert.That(result.RefreshToken, Is.EqualTo("new-refresh"));
}

[Test]
public void RefreshAsync_Should_Throw_When_Already_Revoked()
{
    var token = new RefreshToken { RevokedAt = DateTime.UtcNow.AddMinutes(-1) };

    _tokenService.Setup(x => x.HashToken("raw")).Returns("hash");
    _refreshTokenRepo.Setup(x => x.GetByTokenHashAsync("hash")).ReturnsAsync(token);

    Assert.ThrowsAsync<InvalidCredentialsException>(
        async () => await _authService.RefreshAsync("raw"));
}

[Test]
public void RefreshAsync_Should_Throw_When_Token_Not_Found()
{
    _tokenService.Setup(x => x.HashToken("raw")).Returns("hash");
    _refreshTokenRepo.Setup(x => x.GetByTokenHashAsync("hash"))
        .ReturnsAsync((RefreshToken)null);

    Assert.ThrowsAsync<InvalidCredentialsException>(
        async () => await _authService.RefreshAsync("raw"));
}

[Test]
public async Task ResetPasswordAsync_Should_Update_Password()
{
    var user = new User { Id = 1, Email = "john@test.com" };
    var token = new PasswordResetToken
    {
        User = user,
        UsedAt = null,
        ExpiresAt = DateTime.UtcNow.AddMinutes(30)
    };

    _tokenService.Setup(x => x.HashToken("raw")).Returns("hash");
    _passwordResetTokenRepo.Setup(x => x.GetByTokenHashAsync("hash"))
        .ReturnsAsync(token);

    await _authService.ResetPasswordAsync("raw", "NewPassword123!");

    Assert.That(token.UsedAt, Is.Not.Null);
    Assert.That(user.PasswordHash, Is.Not.Null.And.Not.Empty);
    _passwordResetTokenRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
}
[Test]
public async Task ForgetPasswordAsync_Should_Return_When_User_Not_Found()
{
    _userRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
        .ReturnsAsync((User)null);

    await _authService.ForgetPasswordAsync("missing@test.com");

    _passwordResetTokenRepo.Verify(
        x => x.AddAsync(It.IsAny<PasswordResetToken>()),
        Times.Never);
}

[Test]
public async Task ForgetPasswordAsync_Should_Create_Reset_Token()
{
    var user = new User
    {
        Id = 1,
        Email = "john@test.com"
    };

    _userRepo.Setup(x => x.GetByEmailAsync(user.Email))
        .ReturnsAsync(user);

    _tokenService.Setup(x => x.GenerateRefreshToken())
        .Returns("reset-token");

    _tokenService.Setup(x => x.HashToken("reset-token"))
        .Returns("hash");

    await _authService.ForgetPasswordAsync(user.Email);

    _passwordResetTokenRepo.Verify(
        x => x.AddAsync(It.IsAny<PasswordResetToken>()),
        Times.Once);

    _emailService.Verify(
        x => x.SendPasswordResetAsync(user.Email, "reset-token"),
        Times.Once);
}

[Test]
public void ResetPasswordAsync_Should_Throw_When_Token_Not_Found()
{
    _tokenService.Setup(x => x.HashToken("raw"))
        .Returns("hash");

    _passwordResetTokenRepo.Setup(x => x.GetByTokenHashAsync("hash"))
        .ReturnsAsync((PasswordResetToken)null);

    Assert.ThrowsAsync<NotFoundException>(
        async () => await _authService.ResetPasswordAsync("raw", "NewPass123"));
}

[Test]
public void ResetPasswordAsync_Should_Throw_When_Already_Used()
{
    var token = new PasswordResetToken
    {
        UsedAt = DateTime.UtcNow
    };

    _tokenService.Setup(x => x.HashToken("raw"))
        .Returns("hash");

    _passwordResetTokenRepo.Setup(x => x.GetByTokenHashAsync("hash"))
        .ReturnsAsync(token);

    Assert.ThrowsAsync<TokenAlreadyUsedException>(
        async () => await _authService.ResetPasswordAsync("raw", "Password123"));
}

[Test]
public void ResetPasswordAsync_Should_Throw_When_Expired()
{
    var token = new PasswordResetToken
    {
        UsedAt = null,
        ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
    };

    _tokenService.Setup(x => x.HashToken("raw"))
        .Returns("hash");

    _passwordResetTokenRepo.Setup(x => x.GetByTokenHashAsync("hash"))
        .ReturnsAsync(token);

    Assert.ThrowsAsync<TokenExpiredException>(
        async () => await _authService.ResetPasswordAsync("raw", "Password123"));
}
}