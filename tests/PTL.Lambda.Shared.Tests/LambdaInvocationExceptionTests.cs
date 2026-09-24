using PTL.Lambda.Shared;

namespace PTL.Lambda.Shared.Tests;

public class LambdaInvocationExceptionTests
{
    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsBoth()
    {
        var inner = new InvalidOperationException("inner");

        var exception = new LambdaInvocationException("outer", inner);

        Assert.Equal("outer", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void Constructor_WithMessageOnly_SetsMessage()
    {
        var exception = new LambdaInvocationException("outer");

        Assert.Equal("outer", exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Constructor_Parameterless_CreatesInstance() =>
        Assert.NotNull(new LambdaInvocationException());
}
