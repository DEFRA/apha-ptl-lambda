namespace PTL.Lambda.Shared;

/// <summary>Wraps a failed Lambda invocation with the function name and elapsed time, preserving the original exception as <see cref="Exception.InnerException"/>.</summary>
public sealed class LambdaInvocationException : Exception
{
    public LambdaInvocationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public LambdaInvocationException(string message)
        : base(message)
    {
    }

    public LambdaInvocationException()
    {
    }
}
