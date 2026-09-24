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
            Log.Error(
                exception,
                "Database connection to database {DatabaseName} on host {DatabaseHost} failed",
                options.Name, options.Host);
            throw;
        }

        Log.Information(
            "Database connection to database {DatabaseName} on host {DatabaseHost} is successful",
            options.Name, options.Host);
    }
}
