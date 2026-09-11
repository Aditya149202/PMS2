using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace UserAuthService.ExceptionMiddleware;
public class UserExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public UserExceptionMiddleware(RequestDelegate next,ILogger<UserExceptionMiddleware> logger,IHostEnvironment env){
        _next=next;
        _logger=logger;
        _env=env;
    }
    public async Task InvokeAsync(HttpContext Context)
    {
        try
        {
            await _next(Context);
        }catch(Exception e)
        {
            await HandleExceptionAsync(Context,e);
        }
    }

    private async Task HandleExceptionAsync(HttpContext Context,Exception e)
    {
        var traceId=Activity.Current?.Id??Context.TraceIdentifier;

        object problem;
        int statusCode;

        switch (e)
        {
            case TokenAlreadyUsedException tokenAlreadyUsedEx:
                statusCode = tokenAlreadyUsedEx.StatusCode;
                _logger.LogWarning(e,"token already used. TraceId={traceId}",traceId);
                problem = new ProblemDetails
                    {
                        Status = statusCode,
                        Title = ReasonPhraseFor(statusCode),
                        Detail= tokenAlreadyUsedEx.Message,
                        Extensions = { ["traceId"] = traceId }
                    };
                break;

            case TokenExpiredException tokenExpiredEx:
                statusCode = tokenExpiredEx.StatusCode;
                _logger.LogWarning(e,"token expired. TraceId={traceId}",traceId);
                problem = new ProblemDetails
                    {
                        Status = statusCode,
                        Title = ReasonPhraseFor(statusCode),
                        Detail= tokenExpiredEx.Message,
                        Extensions = { ["traceId"] = traceId }
                    };
                break;
            case AppValidationException validationEx:
                statusCode = validationEx.StatusCode;
                _logger.LogWarning(e,"Validation failure. TraceId={traceId}",traceId);
                problem = new ValidationProblemDetails(validationEx.Errors)
                    {
                        Status = statusCode,
                        Title = "One or more validation errors occurred.",
                        Extensions = { ["traceId"] = traceId }
                    };
                break;
            case AppException appException:
                statusCode=appException.StatusCode;
                _logger.LogWarning(e,"Application exception. TraceId={traceId}",traceId);
                problem= new ProblemDetails
                {
                    Status=statusCode,
                    Title=ReasonPhraseFor(statusCode),
                    Detail = appException.Message,
                    Extensions={["traceId"]=traceId}
                };
                break;
            default:
                statusCode=StatusCodes.Status500InternalServerError;
                _logger.LogWarning(e,"unhandled expection.  TraceId={traceId}",traceId);
                problem = new ProblemDetails
                    {
                        Status = statusCode,
                        Title = "An unexpected error occurred.",
                        // Never leak stack traces / internal messages to the client in non-dev envs.
                        Detail = _env.IsDevelopment() ? e.ToString() : "Please contact support if the problem persists.",
                        Extensions = { ["traceId"] = traceId }
                    };
                break;

            
        }
        Context.Response.ContentType = "application/problem+json";
        Context.Response.StatusCode = statusCode;

        var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await Context.Response.WriteAsync(json);
    }
    private static string ReasonPhraseFor(int statusCode){ 
        string output;
        switch (statusCode)
        {
            case (int)HttpStatusCode.NotFound:
                output= "Resource not found.";
                break;
            case (int)HttpStatusCode.Conflict: 
                output= "Request conflicts with current state.";
                break;
            case (int)HttpStatusCode.Unauthorized: 
            output="Authentication failed.";
            break;
            case (int)HttpStatusCode.Forbidden: 
                output= "Not permitted.";
                break;
            
            default:
            output= "Request could not be completed.";
            break;
            
        }
        return output;
    }
        
}