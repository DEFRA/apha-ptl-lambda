using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using PTL.Lambda.DeleteAttachments;
using PTL.Lambda.Shared;

Func<object, ILambdaContext, Task<CleanupResult>> handler = DeleteAttachmentsFunction.FunctionHandler;

// The self-contained published executable hosts this runtime-API loop directly,
// so it IS the container's entrypoint - no separate "bootstrap" shell script needed.
using var bootstrap = LambdaBootstrapBuilder.Create(handler, new DefaultLambdaJsonSerializer()).Build();
await bootstrap.RunAsync();
