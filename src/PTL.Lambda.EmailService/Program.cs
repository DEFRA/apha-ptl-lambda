using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using PTL.Lambda.EmailService;
using PTL.Lambda.Shared;

Func<object, ILambdaContext, Task<CleanupResult>> handler = EmailServiceFunction.FunctionHandler;

using var bootstrap = LambdaBootstrapBuilder.Create(handler, new DefaultLambdaJsonSerializer()).Build();
await bootstrap.RunAsync();
