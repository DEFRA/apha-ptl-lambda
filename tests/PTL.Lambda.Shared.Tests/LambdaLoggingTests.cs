using Amazon.Lambda.TestUtilities;
using Serilog;

namespace PTL.Lambda.Shared.Tests;

public class LambdaLoggingTests
{
    [Fact]
    public void Configure_IsIdempotent()
    {
        LambdaLogging.Configure();
        var loggerAfterFirstCall = Log.Logger;

        LambdaLogging.Configure();

        Assert.Same(loggerAfterFirstCall, Log.Logger);
    }

    [Fact]
    public async Task InvokeAsync_ReturnsResult_AndLogsCompletionLine()
    {
        var context = new TestLambdaContext { AwsRequestId = Guid.NewGuid().ToString() };
        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            var result = await LambdaLogging.InvokeAsync(
                "TestFunction", context, () => Task.FromResult("done"));

            Assert.Equal("done", result);
            var output = writer.ToString();
            Assert.Contains("TestFunction", output);
            Assert.Contains(context.AwsRequestId, output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task InvokeAsync_RethrowsAndLogsFailure_WhenInvocationThrows()
    {
        var context = new TestLambdaContext { AwsRequestId = Guid.NewGuid().ToString() };
        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                LambdaLogging.InvokeAsync<string>("TestFunction", context, () =>
                    throw new InvalidOperationException("boom")));

            var output = writer.ToString();
            Assert.Contains("TestFunction", output);
            Assert.Contains("boom", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
