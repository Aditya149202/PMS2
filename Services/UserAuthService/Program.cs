using UserAuthService.Data;
using Microsoft.EntityFrameworkCore;
using UserAuthService.Repositories.Interfaces;
using UserAuthService.Repositories.Implementations;
using UserAuthService.Services.Implementations;
using UserAuthService.Services.Interfaces;
using UserAuthService.ExceptionMiddleware;
using Microsoft.AspNetCore.Identity;
using UserAuthService.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;
using Swashbuckle.AspNetCore;
using Microsoft.OpenApi;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<UsersDbContext>(u=>u.UseSqlServer(builder.Configuration.GetConnectionString("UsersDB")));
builder.Services.AddScoped<IUserRepository,UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository,RefreshTokenRepository>();
builder.Services.AddScoped<IRoleRepository,RoleRepository>();
builder.Services.AddScoped<IPasswordResetTokenRepository,PasswordResetTokenRepository>();
builder.Services.AddScoped<IAuthService,AuthService>();
builder.Services.AddScoped<ITokenService,TokenService>();
builder.Services.AddScoped<IEmailService,EmailService>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();


// 2. Modern OpenAPI Generation with the Transformer injected

builder.Services.AddControllers();

// --- Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    const string schemeId="Bearer";
    options.AddSecurityDefinition(schemeId,new OpenApiSecurityScheme
    {
        Type=SecuritySchemeType.Http,
        Scheme="bearer",
        BearerFormat="JWT",
        Description="Enter your JWT token"
    });
    options.AddSecurityRequirement(document=>new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference(schemeId,document)]=[]
    });
});

var rsa=RSA.Create();
rsa.ImportFromPem(builder.Configuration["Jwt:PublicKey"]!);
//var jwtKey = builder.Configuration["Jwt:Key"]!;
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
                context.HandleResponse(); // stop the default 401 body
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


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userRepo = services.GetRequiredService<IUserRepository>();
    var roleRepo = services.GetRequiredService<IRoleRepository>();
    var hasher = services.GetRequiredService<IPasswordHasher<User>>();
    var config = services.GetRequiredService<IConfiguration>();

    var adminEmail = config["AdminSeed:Email"]!;
    var existingAdmin = await userRepo.GetByEmailAsync(adminEmail);

    if (existingAdmin is null)
    {
        var adminRole = await roleRepo.GetByNameAsync("ADMIN")
            ?? throw new InvalidOperationException("ADMIN role not seeded — check Roles HasData.");

        var admin = new User
        {
            Name = "Admin",
            Email = adminEmail,
            RoleId = adminRole.Id,
            CreatedAt = DateTime.UtcNow
        };
        admin.PasswordHash = hasher.HashPassword(admin, config["AdminSeed:Password"]!);

        await userRepo.AddAsync(admin);
        await userRepo.SaveChangesAsync();
    }
}
// Configure the HTTP request pipeline.
app.UseMiddleware<UserExceptionMiddleware>(); // your existing exception middleware — register if not already


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    //app.MapScalarApiReference();
    app.UseSwaggerUI();
}
app.Run();