using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using SupplierInventoryService.Data;
using SupplierInventoryService.Repositories.Implementations;
using SupplierInventoryService.Repositories.Interfaces;
using SupplierInventoryService.Services.Implementations;
using SupplierInventoryService.Services.Interfaces;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using SupplierInventoryService.ExceptionMiddleware;
using SupplierInventoryService.Entities;
using SupplierInventoryService.Repositories;
using SupplierInventoryService.Services;
using Swashbuckle.AspNetCore;
using Microsoft.OpenApi;
using System.Security.Cryptography;
using SupplierInventoryService.Auth;
var builder = WebApplication.CreateBuilder(args);

// --- EF Core ---
builder.Services.AddDbContext<SupplierInventoryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SupplierInventoryDb")));

// --- Repositories ---
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<IDrugRepository, DrugRepository>();
builder.Services.AddScoped<IStockReservationRepository, StockReservationRepository>();
builder.Services.AddScoped<ISalesRepository, SalesRepository>();
builder.Services.AddScoped<IProcessedEventRepository, ProcessedEventRepository>();

// --- Services ---
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IDrugService, DrugService>();
builder.Services.AddScoped<IInternalService, InternalService>();
builder.Services.AddScoped<IReportService,ReportService>();


// --- Controllers ---
builder.Services.AddControllers();

// --- Swagger ---
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
var rsa=RSA.Create();
rsa.ImportFromPem(builder.Configuration["Jwt:PublicKey"]!);
// --- JWT — VALIDATION ONLY (this service never generates tokens, per LLD) ---
//var jwtKey = builder.Configuration["Jwt:Key"]!; // MUST match UserAuthService's Jwt:Key exactly
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(rsa),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
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
    }).AddScheme<ApiKeyAuthenticationOptions,ApiKeyAuthenticationHandler>(ApiKeyAuthenticationOptions.SchemeName,options=>{});

builder.Services.AddAuthorization();

var app = builder.Build();

// --- Middleware pipeline ---
app.UseMiddleware<SupplierInventoryExceptionMiddleware>(); // your existing exception middleware for this service

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();