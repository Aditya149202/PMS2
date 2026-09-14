using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using UserAuthService.Services.Implementations;
using UserAuthService.Entities;

namespace UserAuthService.Tests.Services;

[TestFixture]
public class TokenServiceTests
{
    private TokenService _tokenService;

    [SetUp]
    public void Setup()
    {
        var configValues = new Dictionary<string, string?>
        {
            { "Jwt:Key", "this-is-a-test-signing-key-at-least-32-chars-long" },
            { "Jwt:Issuer", "TestIssuer" },
            { "Jwt:Audience", "TestAudience" }
        };

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        _tokenService = new TokenService(config);
    }

    [Test]
    public void GenerateAccessToken_Should_Include_Correct_Claims()
    {
        var user = new User
        {
            Id = 42,
            Email = "john@test.com",
            Role = new Role { Id = 1, Name = "DOCTOR" }
        };

        var token = _tokenService.GenerateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.That(jwt.Subject, Is.EqualTo("42"));
        Assert.That(jwt.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "DOCTOR"), Is.True);
        Assert.That(jwt.Issuer, Is.EqualTo("TestIssuer"));
    }

    [Test]
    public void GenerateAccessToken_Should_Expire_In_Future()
    {
        var user = new User { Id = 1, Role = new Role { Name = "DOCTOR" } };
        var token = _tokenService.GenerateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.That(jwt.ValidTo, Is.GreaterThan(DateTime.UtcNow));
    }

    [Test]
    public void GenerateRefreshToken_Should_Produce_Different_Values_Each_Call()
    {
        var token1 = _tokenService.GenerateRefreshToken();
        var token2 = _tokenService.GenerateRefreshToken();

        Assert.That(token1, Is.Not.EqualTo(token2));
    }

    [Test]
    public void HashToken_Should_Be_Deterministic()
    {
        var hash1 = _tokenService.HashToken("same-input");
        var hash2 = _tokenService.HashToken("same-input");

        Assert.That(hash1, Is.EqualTo(hash2));
    }

    [Test]
    public void HashToken_Should_Differ_For_Different_Inputs()
    {
        var hash1 = _tokenService.HashToken("input-one");
        var hash2 = _tokenService.HashToken("input-two");

        Assert.That(hash1, Is.Not.EqualTo(hash2));
    }
}