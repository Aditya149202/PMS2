using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace SupplierInventoryService.Auth;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "InternalApiKey";
}

// Separate scheme from the existing JWT bearer scheme — this one authenticates
// OrderService itself, not a human. No role claim, since "is this OrderService"
// is a yes/no question, not a role-based one.
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private const string HeaderName = "X-Internal-Api-Key";
    private readonly IConfiguration _config;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration config)
        : base(options, logger, encoder)
    {
        _config = config;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var providedKey))
            return Task.FromResult(AuthenticateResult.Fail($"Missing {HeaderName} header."));

        var expectedKey = _config["InternalApi:Key"];
        if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
            return Task.FromResult(AuthenticateResult.Fail("Invalid internal API key."));

        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "OrderService") }, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}