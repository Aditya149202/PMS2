using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Microsoft.OpenApi;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
var builder = WebApplication.CreateBuilder(args);


builder.Services.AddHttpClient();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    const string schemeId = "Bearer";

    options.AddSecurityDefinition(schemeId, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter your JWT access token."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference(schemeId, document)] = []
    });
});

var rsa = RSA.Create();
rsa.ImportFromPem(builder.Configuration["Jwt:PublicKey"]!.ToCharArray());
var signingKey = new RsaSecurityKey(rsa);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = signingKey,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/problem+json";
                var problem = new
                {
                    type = "https://tools.ietf.org/html/rfc7807",
                    title = "Unauthorized",
                    status = 401,
                    detail = "A valid access token is required.",
                    traceId = context.HttpContext.TraceIdentifier
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json";
                var problem = new
                {
                    type = "https://tools.ietf.org/html/rfc7807",
                    title = "Forbidden",
                    status = 403,
                    detail = "You do not have permission to perform this action.",
                    traceId = context.HttpContext.TraceIdentifier
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
            }
        };

        
    });

builder.Services.AddAuthorization();



builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
var app = builder.Build();
app.MapGet("/gateway.json", async (IHttpClientFactory httpClientFactory, IConfiguration config) =>
{
    var sources = config.GetSection("SwaggerAggregator:SourceUrls").Get<string[]>() ?? Array.Empty<string>();

    var client = httpClientFactory.CreateClient();
    JsonObject? merged = null;

    foreach (var url in sources)
    {
        var json = await client.GetStringAsync(url);
        var doc = JsonNode.Parse(json)!.AsObject();

        if (merged is null)
        {
            merged = doc;
            merged["info"] = new JsonObject { ["title"] = "PMS Combined API", ["version"] = "v1" };
            continue;
        }

        var mergedPaths = merged["paths"]!.AsObject();
        foreach (var (path, value) in doc["paths"]!.AsObject())
            mergedPaths[path] = value?.DeepClone();

        var mergedSchemas = merged["components"]!["schemas"]!.AsObject();
        if (doc["components"]?["schemas"] is JsonObject schemas)
            foreach (var (name, value) in schemas)
                mergedSchemas[name] = value?.DeepClone();
    }

    return Results.Text(merged!.ToJsonString(), "application/json");
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/gateway.json", "PMS Combined API v1");
    });
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization(); 
app.MapReverseProxy();
app.Run();