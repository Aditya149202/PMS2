using UserAuthService.Entities;
using Microsoft.AspNetCore.Http;
namespace UserAuthService.ExceptionMiddleware;

public abstract class AppException : Exception
{
    public int  StatusCode{get;}

    public AppException(string message,int StatusCode) : base(message)
    {
        this.StatusCode=StatusCode;
    }
}
public class NotFoundException(string message) : AppException(message, StatusCodes.Status404NotFound)
{

}

public class  ConflictException : AppException
{
    public ConflictException(string message) : base(message, StatusCodes.Status409Conflict)
    {
        
    }
}

public class AppValidationException : AppException
{
    public IDictionary<string, string[]> Errors { get; }
    public AppValidationException(IDictionary<string,string[]> Errors):base("One or more validation errors occured", StatusCodes.Status400BadRequest)
    {
        this.Errors=Errors;
    }
}

public class InvalidCredentialsException(string message = "Invalid credentials.")
    : AppException(message, StatusCodes.Status401Unauthorized)
{
}

public class TokenExpiredException(string message) : AppException(message, StatusCodes.Status410Gone)

{
    
}

public class TokenAlreadyUsedException(string message) : AppException(message,StatusCodes.Status409Conflict){}