using Microsoft.AspNetCore.Http;

namespace SupplierInventoryService.ExceptionMiddleware;

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

/// <summary>404 — drug, supplier, or reservation does not exist.</summary>
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
/// 409 — not enough quantity_in_stock to satisfy a reservation request.
/// This is the exception OrderService's synchronous reserve call should surface
/// back to the caller as a StockUnavailableException on that side.
/// </summary>
public class InsufficientStockException : ConflictException
{
    public InsufficientStockException(string message) : base(message) { }
}

/// <summary>
/// 409 — attempted to commit/release a StockReservation that is not ACTIVE
/// (e.g. double-processing an OrderPickedUp event outside the ProcessedEvents guard).
/// </summary>
public class InvalidReservationStateException : ConflictException
{
    public InvalidReservationStateException(string message) : base(message) { }
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

    public AppValidationException(string field, string error)
        : this(new Dictionary<string, string[]> { { field, new[] { error } } }) { }
}
