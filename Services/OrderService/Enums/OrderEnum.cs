namespace OrderService.Enums;

public enum OrderStatus
{
    NEW,
    VERIFIED,
    COMPLETED,
    CANCELLED
}

public enum PaymentIntentStatus
{
    CREATED,
    PAID,
    FAILED,
    EXPIRED
}

public enum CancelledBy
{
    ADMIN,
    SYSTEM,

    DOCTOR
}