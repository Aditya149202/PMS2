using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SupplierInventoryService.ExceptionMiddleware;

namespace SupplierInventoryService.ExceptionMiddleware;

    /// <summary>
    /// Single place where unhandled exceptions are turned into a consistent JSON
    /// error shape (RFC 7807 ProblemDetails). Must be registered FIRST in the
    /// pipeline — before UseAuthentication()/UseAuthorization().
    ///
    /// Registration (Program.cs):
    ///   app.UseMiddleware&lt;GlobalExceptionMiddleware&gt;();
    ///   app.UseAuthentication();
    ///   app.UseAuthorization();
    /// </summary>
public class SupplierInventoryExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SupplierInventoryExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public SupplierInventoryExceptionMiddleware(
        RequestDelegate next,
        ILogger<SupplierInventoryExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        object problem;
        int statusCode;

        switch (ex)
        {
            case AppValidationException validationEx:
                statusCode = validationEx.StatusCode;
                _logger.LogWarning(ex, "Validation failure. TraceId={TraceId}", traceId);
                problem = new ValidationProblemDetails(validationEx.Errors)
                {
                    Status = statusCode,
                    Title = "One or more validation errors occurred.",
                    Extensions = { ["traceId"] = traceId }
                };
                break;

            // Stock/reservation conflicts are expected, business-level outcomes
            // (a doctor tried to buy more than is in stock) — not bugs.
            case InsufficientStockException or InvalidReservationStateException:
                var appEx1 = (AppException)ex;
                statusCode = appEx1.StatusCode;
                _logger.LogWarning(ex, "Stock/reservation conflict. TraceId={TraceId}", traceId);
                problem = new ProblemDetails
                {
                    Status = statusCode,
                    Title = "Request conflicts with current stock/reservation state.",
                    Detail = appEx1.Message,
                    Extensions = { ["traceId"] = traceId }
                };
                break;

            case AppException appEx:
                statusCode = appEx.StatusCode;
                _logger.LogWarning(ex, "Handled application exception. TraceId={TraceId}", traceId);
                problem = new ProblemDetails
                {
                    Status = statusCode,
                    Title = ReasonPhraseFor(statusCode),
                    Detail = appEx.Message,
                    Extensions = { ["traceId"] = traceId }
                };
                break;

            default:
                statusCode = StatusCodes.Status500InternalServerError;
                _logger.LogError(ex, "Unhandled exception. TraceId={TraceId}", traceId);
                problem = new ProblemDetails
                {
                    Status = statusCode,
                    Title = "An unexpected error occurred.",
                    Detail = _env.IsDevelopment() ? ex.ToString() : "Please contact support if the problem persists.",
                    Extensions = { ["traceId"] = traceId }
                };
                break;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private static string ReasonPhraseFor(int statusCode) => statusCode switch
    {
        (int)HttpStatusCode.NotFound => "Resource not found.",
        (int)HttpStatusCode.Conflict => "Request conflicts with current state.",
        (int)HttpStatusCode.Unauthorized => "Authentication failed.",
        (int)HttpStatusCode.Forbidden => "Not permitted.",
        _ => "Request could not be completed."
    };
}