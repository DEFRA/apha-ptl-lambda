using Microsoft.Extensions.Configuration;

namespace PTL.Lambda.Shared;

public static class StartupChecks
{
    /// <summary>
    /// Fails fast at invocation if any DB config value is missing - a clear,
    /// immediate error beats a handler that starts and only fails later on
    /// first database use. Host/Name/User/Password come from Secrets Manager
    /// as four separate Lambda environment variables (no pre-built connection
    /// string), so this validates and packages them for SqlConnectionFactory to
    /// compose. TrustServerCertificate defaults to false (secure by default,
    /// correct for RDS's valid certificate) and should only be set true locally,
    /// for a self-signed dev SQL Server certificate.
    /// </summary>
    public static DatabaseOptions RequireDatabaseOptions(IConfiguration configuration)
    {
        var host = configuration["Database:Host"];
        var name = configuration["Database:Name"];
        var user = configuration["Database:User"];
        var password = configuration["Database:Password"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Database:Host, Database:Name, Database:User and Database:Password must all be configured. " +
                "Locally, set the Database__Host / Database__Name / Database__User / Database__Password " +
                "environment variables; in a deployed environment, check the Lambda function's environment " +
                "variable wiring to Secrets Manager.");
        }

        var trustServerCertificate = configuration.GetValue("Database:TrustServerCertificate", false);

        return new DatabaseOptions(host, name, user, password, trustServerCertificate);
    }
}
