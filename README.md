# PTL Lambda

AWS Lambda functions for the PT-LIMS (Proficiency Testing) platform's scheduled background
processing, deployed as containerised images triggered by Amazon EventBridge.

## Functions

| Function | Description |
|---|---|
| `PTL.Lambda.DeleteAttachments` | Deletes expired internal/published result attachments |
| `PTL.Lambda.DeleteConsultant` | Removes consultant records that are no longer required |
| `PTL.Lambda.RemoveCustomerData` | GDPR-compliant scheduled customer data cleanup |
| `PTL.Lambda.EmailService` | Sends PT-LIMS email notifications via GOV.UK Notify |
| `PTL.Lambda.Shared` | Shared DB connection factory, startup config checks, Lambda config bootstrap |

**Current status: scaffolding.** Each handler is a stub that validates its required configuration
(database or GOV.UK Notify options) and returns a sample result describing what it would do.

## Prerequisites

- [.NET SDK 10.0](https://dotnet.microsoft.com/download)
- Docker (Linux containers) - required to build/test the Lambda container images locally
- Git

## Getting started

1. Clone the repository and open `PTL.Lambda.slnx`.
2. Restore local tooling (required once per clone, powers the pre-commit hook):
   ```powershell
   dotnet tool restore
   dotnet husky install
   ```
3. Build the solution:
   ```powershell
   dotnet build PTL.Lambda.slnx
   ```
4. Run the tests:
   ```powershell
   dotnet test PTL.Lambda.slnx
   ```

Each function's handler is a plain static method - test it directly (see `tests/*.Tests`) without
needing the Lambda runtime. To exercise a function end-to-end locally, build its Docker image and run
it with the [AWS Lambda Runtime Interface Emulator](https://github.com/aws/aws-lambda-runtime-interface-emulator).

## Why container images

Every function deploys as a **Lambda container image** (`public.ecr.aws/lambda/provided:al2023` base),
keeping every function on .NET 10 without waiting on an AWS-managed runtime release. Lambda only
supports Linux containers, so every Dockerfile targets Linux (`linux-x64`) and CI builds on
`ubuntu-latest` runners.

## Database connection

The cleanup functions connect to the PT-LIMS SQL Server (RDS) via `PTL.Lambda.Shared/SqlConnectionFactory`
(Dapper on top of `Microsoft.Data.SqlClient`), configured from four environment variables rather than a
connection string:

| Config key | Lambda environment variable |
|---|---|
| `Database:Host` | `Database__Host` |
| `Database:Name` | `Database__Name` |
| `Database:User` | `Database__User` |
| `Database:Password` | `Database__Password` |
| `Database:TrustServerCertificate` | `Database__TrustServerCertificate` (optional, default `false`) |

These should reference Secrets Manager (or SSM Parameter Store) values via the Lambda function's
configuration - nothing DB-related is ever baked into the image. Missing/blank values fail the
invocation immediately (visible in CloudWatch Logs) rather than failing silently later.

`EmailService` instead requires a `Notify:ApiKey` value (`Notify__ApiKey`) for sending via
[GOV.UK Notify](https://www.notifications.service.gov.uk/).

## Logging & correlation IDs

Every function logs structured JSON to stdout via Serilog (`PTL.Lambda.Shared/LambdaLogging`, compact
formatter, console sink only - no file sinks). Lambda ships stdout/stderr to CloudWatch Logs
automatically, so there is no `awslogs` driver or log-group wiring to configure in the function itself.
To follow a function's logs, tail its CloudWatch Logs group (`/aws/lambda/<function-name>`) - locally,
just read the console output of the Docker/RIE process.

**Correlation is automatic, not manual.** Each `FunctionHandler` runs its body through
`LambdaLogging.InvokeAsync(functionName, context, ...)`, which pushes `FunctionName` and the
invocation's `AwsRequestId` into the Serilog `LogContext` (so every log line during the invocation
carries them) and logs one structured completion/failure line with the elapsed time - the Lambda
equivalent of the web apps' one-line-per-request log. `AwsRequestId` is assigned by AWS for every
invocation; there is no inbound header to read and nothing for a caller to supply, since these
functions are triggered by EventBridge schedules rather than by another service's HTTP call. To trace
one invocation end-to-end, filter/grep its CloudWatch Logs group for that `AwsRequestId`.

### Example: logging from application code

Once the cleanup/send logic replaces a stub, log with `Serilog.Log` (or inject `Serilog.ILogger` if the
type takes a constructor) and a structured message template - never string interpolation - so values
stay queryable as real JSON fields rather than being flattened into the message text:

```csharp
public static class DeleteAttachmentsFunction
{
    public static Task<CleanupResult> FunctionHandler(object input, ILambdaContext context) =>
        LambdaLogging.InvokeAsync(nameof(DeleteAttachmentsFunction), context, async () =>
        {
            var configuration = LambdaConfiguration.Build();
            var options = StartupChecks.RequireDatabaseOptions(configuration);

            var connectionFactory = new SqlConnectionFactory(configuration);
            using var connection = connectionFactory.CreateConnection();

            var deletedCount = await AttachmentCleanup.DeleteExpiredAsync(connection);
            Log.Information("Deleted {DeletedCount} expired attachments from {Database}", deletedCount, options.Name);

            return new CleanupResult("DeleteAttachments", "Completed", $"Deleted {deletedCount} attachments.", DateTime.UtcNow);
        });
}
```

Because `FunctionName` and `AwsRequestId` are already in the Serilog `LogContext` for the invocation (see
above), the `Log.Information` line is automatically enriched with both - no need to pass them around
manually. The resulting CloudWatch Logs Insights query to see everything logged for one invocation:

```
fields @timestamp, AwsRequestId, DeletedCount, @message
| filter AwsRequestId = "…"
| sort @timestamp asc
```

## CI/CD

[`.github/workflows/build-test-publish-images.yml`](.github/workflows/build-test-publish-images.yml):

1. **`changes`** - detects which function(s) changed (a change under `PTL.Lambda.Shared` affects all).
2. **`quality`** - restores, format-checks, builds and tests the whole solution.
3. **`sonarcloud`** *(optional, gated on the `SONAR_ENABLED` repo variable)*.
4. **`container-validation`** - builds only the affected functions' Docker images.
5. **`publish`** *(push to `main` / manual dispatch only)* - pushes the validated image to that
   function's own ECR repository as `sha-<12-char-sha>-<run-id>-<run-attempt>`.
6. **`gate`** - a single required status check that fails if any of the above didn't succeed.

Only functions whose code changed are built and published on a given run.

### Required GitHub configuration

Each function publishes into **its own ECR repository**:

| Name | Kind | Example value |
|---|---|---|
| `ECR_DELETE_ATTACHMENTS_REPOSITORY` | repo variable | `apha/ptl-delete-attachments` |
| `ECR_DELETE_CONSULTANT_REPOSITORY` | repo variable | `apha/ptl-delete-consultant` |
| `ECR_REMOVE_CUSTOMER_DATA_REPOSITORY` | repo variable | `apha/ptl-delete-customer-data` |
| `ECR_EMAIL_SERVICE_REPOSITORY` | repo variable | `apha/ptl-email-service` |
| `EXPECTED_AWS_ACCOUNT_ID` | repo variable | 12-digit AWS account ID |
| `EXPECTED_AWS_REGION` | repo variable | `eu-west-2` |
| `SONAR_ENABLED` | repo variable | `true` / `false` |
| `SONAR_PROJECT_KEY` / `SONAR_ORGANIZATION` | repo variables | - |
| `AWS_ENV_REGION` | environment secret (`ecr-production`) | `eu-west-2` |
| `AWS_ENV_ACCOUNT` | environment secret (`ecr-production`) | 12-digit AWS account ID |
| `AWS_ENV_OIDC_ROLE` | environment secret (`ecr-production`) | OIDC deployment role name |
| `SONAR_TOKEN` | repo secret | - |

This repo only builds and publishes images to ECR - it does not deploy them to a Lambda function or
provision infrastructure (EventBridge rules, IAM roles, etc.); that is handled separately.
