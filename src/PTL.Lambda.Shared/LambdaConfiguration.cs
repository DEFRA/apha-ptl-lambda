using Microsoft.Extensions.Configuration;

namespace PTL.Lambda.Shared;

/// <summary>
/// Builds configuration purely from environment variables (Database__Host style,
/// matching PTL.Api's ECS convention) - Lambda has no appsettings.json equivalent.
/// </summary>
public static class LambdaConfiguration
{
    public static IConfiguration Build() =>
        new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
}
