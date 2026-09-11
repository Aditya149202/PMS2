using Microsoft.AspNetCore.Http;

namespace OrderService.ExceptionMiddleware;

    /// <summary>
    /// Base type for all exceptions that GlobalExceptionMiddleware knows how to
    /// translate into a specific HTTP status code. Anything NOT derived from this
    /// is treated as an unexpected/unhandled error (500) and logged in full.
    /// </summary>
public abstract class AppException : Exception
{
    public int StatusCode { get; }

    protected AppException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
}

/// <summary>404 — order, payment intent, or order item does not exist.</summary>
public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message, StatusCodes.Status404NotFound) { }
}

/// <summary>409 — generic state conflict not covered by a more specific type below.</summary>
public class ConflictException : AppException
{
    public ConflictException(string message) : base(message, StatusCodes.Status409Conflict) { }
}

/// <summary>
/// 409 — an operation was attempted against an order whose current status
/// does not allow it (e.g. pickup requested on an order that is not VERIFIED,
/// or verify requested on an already-CANCELLED order).
/// </summary>
public class InvalidOrderStateException : ConflictException
{
    public InvalidOrderStateException(string message) : base(message) { }
}

/// <summary>
/// 409 — the synchronous stock check-and-reserve call to SupplierInventoryService
/// was rejected (insufficient stock at reservation time). Correctness-critical,
/// so this must surface synchronously and never be silently retried async.
/// </summary>
public class StockUnavailableException : ConflictException
{
    public StockUnavailableException(string message) : base(message) { }
}

/// <summary>
/// 400 — Razorpay webhook signature did not match. Kept distinct from
/// AppValidationException since it's a security check, not a field-shape check.
/// </summary>
public class InvalidPaymentSignatureException : AppException
{
    public InvalidPaymentSignatureException(string message = "Payment signature verification failed.")
        : base(message, StatusCodes.Status400BadRequest) { }
}

/// <summary>400 — field-level validation failures.</summary>
public class AppValidationException : AppException
{
    public IDictionary<string, string[]> Errors { get; }

    public AppValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.", StatusCodes.Status400BadRequest)
    {
        Errors = errors;
    }

      
    
}
