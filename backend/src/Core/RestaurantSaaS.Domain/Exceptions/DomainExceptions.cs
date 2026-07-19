namespace RestaurantSaaS.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public sealed class CrossTenantAccessException : DomainException
{
    public CrossTenantAccessException()
        : base("The current principal attempted to access or modify data belonging to a different tenant.") { }
}

public sealed class InsufficientStockException : DomainException
{
    public InsufficientStockException(string ingredientName, decimal requested, decimal available)
        : base($"Insufficient stock for '{ingredientName}': requested {requested}, available {available}.") { }
}

public sealed class InvalidOrderStateException : DomainException
{
    public InvalidOrderStateException(string message) : base(message) { }
}

public sealed class SubscriptionLockedException : DomainException
{
    public SubscriptionLockedException(string tenantName)
        : base($"Tenant '{tenantName}' is locked due to an expired or past-due subscription.") { }
}
