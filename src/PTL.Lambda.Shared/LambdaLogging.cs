using System.Diagnostics;
using Amazon.Lambda.Core;
using Serilog;
using Serilog.Context;
using Serilog.Formatting.Compact;

namespace PTL.Lambda.Shared;

/// <summary>
/// Configures Serilog once per cold start (compact structured JSON to stdout - Lambda ships that
/// straight to CloudWatch Logs, so there is no awslogs driver or file sink to set up) and wraps a
/// single invocation so every log line, plus the invocation's own outcome line, carries the
/// function name and AwsRequestId. AwsRequestId is Lambda's built-in per-invocation ID - the
/// direct equivalent of the X-Correlation-Id used by the HTTP services in this solution, except it
/// needs no header or middleware: AWS assigns one for every invocation automatically.
/// </summary>
public static class LambdaLogging
{
    private static readonly Lock InitLock = new();
    private static bool _initialized;

    /// <summary>Idempotent: safe to call on every invocation, only configures Serilog once.</summary>
    public static void Configure()
    {
        if (_initialized)
        {
            return;
        }

        lock (InitLock)
        {
            if (_initialized)
            {
                return;
            }

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console(new CompactJsonFormatter())
                .CreateLogger();

            _initialized = true;
        }
    }

    /// <summary>
    /// Runs <paramref name="invocation"/> with <paramref name="functionName"/> and the invocation's
    /// AwsRequestId pushed into the Serilog <see cref="LogContext"/>, then logs one structured line
    /// for its outcome and elapsed time - the Lambda equivalent of ASP.NET Core's
    /// <c>UseSerilogRequestLogging()</c> one-line-per-request log.
    /// </summary>
    public static async Task<TResult> InvokeAsync<TResult>(
        string functionName, ILambdaContext context, Func<Task<TResult>> invocation)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(invocation);

        Configure();

        using (LogContext.PushProperty("FunctionName", functionName))
        using (LogContext.PushProperty("AwsRequestId", context.AwsRequestId))
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await invocation().ConfigureAwait(false);
                Log.Information(
                    "Lambda invocation {FunctionName} completed in {ElapsedMilliseconds} ms",
                    functionName, stopwatch.Elapsed.TotalMilliseconds);
                return result;
            }
            catch (Exception exception)
            {
                Log.Error(
                    exception,
                    "Lambda invocation {FunctionName} failed after {ElapsedMilliseconds} ms",
                    functionName, stopwatch.Elapsed.TotalMilliseconds);
                throw new LambdaInvocationException(
                    $"Lambda invocation {functionName} failed after {stopwatch.Elapsed.TotalMilliseconds} ms",
                    exception);
            }
        }
    }
}
