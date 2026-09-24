using Serilog;

namespace PTL.Lambda.Shared;

/// <summary>
/// Opens a real connection to prove DB connectivity end-to-end (network, TLS, auth) - unlike
/// merely constructing an <see cref="IDbConnectionFactory"/> result, which never touches the
/// network - and logs a single structured line so an EventBridge-scheduled invocation's
/// CloudWatch Logs/console output clearly shows whether the database is reachable.
/// </summary>
public static class DatabaseConnectivity
{
    public static void VerifyConnection(IDbConnectionFactory connectionFactory, DatabaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(options);

        using var connection = connectionFactory.CreateConnection();

        try
        {
            connection.Open();
        }
        catch (Exception exception)
        {
            // Rethrow with DB context instead of logging-then-rethrowing the same exception
            // unchanged (Sonar S2139) - LambdaLogging's own catch-all logs the final exception.
            throw new InvalidOperationException(
                $"Database connection to database '{options.Name}' on host '{options.Host}' failed.",
                exception);
        }

        Log.Information(
            "Database connection to database {DatabaseName} on host {DatabaseHost} is successful",
            options.Name, options.Host);
    }
}
