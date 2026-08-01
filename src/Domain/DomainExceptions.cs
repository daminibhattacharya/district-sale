namespace Domain;

/// <summary>
/// A referenced resource does not exist (e.g. a district or salesperson). Maps to HTTP 404.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

/// <summary>
/// A request violates a business rule given the current state (e.g. adding a duplicate
/// secondary, or removing the primary). Maps to HTTP 409.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>
/// An optimistic-concurrency check failed: the row was changed by someone else since it was read,
/// so the supplied rowversion token no longer matches. The caller should reload and retry.
/// Maps to HTTP 409.
/// </summary>
public sealed class ConcurrencyException : Exception
{
    public ConcurrencyException(string message) : base(message) { }
}
