# PTL Lambda

AWS Lambda functions for the PT-LIMS (Proficiency Testing) platform - replacing the legacy scheduled
Windows Services from `proficiency-testing-2026-08-17` with EventBridge-triggered, containerised Lambda
functions, per HLD/HLSA (DD010: *Scheduled Windows Services -> AWS Lambda + Amazon EventBridge*).

## Projects

| Project | Replaces (legacy Windows Service) | Description |
|---|---|---|
| `src/PTL.Lambda.DeleteAttachments` | `ProficiencyTestingDeleteAttachmentsService` | Deletes expired internal/published result attachments |
| `src/PTL.Lambda.DeleteConsultant` | `ProficiencyTestingDeleteConsultantService` | Removes consultant records that are no longer required |
| `src/PTL.Lambda.RemoveCustomerData` | `ProficiencyTestingRemoveCustomerDataService` | GDPR-compliant scheduled customer data cleanup |
| `src/PTL.Lambda.EmailService` | `ProficiencyTestingEmailService` | Sends the 23 PT-LIMS email templates via Microsoft Graph API (replaces legacy SMTP) |
| `src/PTL.Lambda.Shared` | - | Shared DB connection factory, startup config checks, Lambda config bootstrap - used by every function above |

Each `tests/*.Tests` project mirrors its `src/*` counterpart.

**Current status: scaffolding.** Every handler (`Function.cs`) is a stub - it validates its required
configuration (database or Graph API options) and returns a sample `CleanupResult` message describing
what it would do. Business logic must be ported from the corresponding legacy service in
`proficiency-testing-2026-08-17/` before these functions do real work; see the `// TODO` comment at the
top of each `Function.cs`.

## Prerequisites

- [.NET SDK 10.0](https://dotnet.microsoft.com/download)
- Docker (Linux containers) - required to build/test the Lambda container images locally
- Git

## Getting started

1. Clone the repository and open [`PTL.Lambda.slnx`](PTL.Lambda.slnx).
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

Each `Function` class is a plain C# class with a `FunctionHandler(object input, ILambdaContext context)`
method - test it directly (see `tests/*.Tests/FunctionTests.cs`) without needing the Lambda runtime.
`Program.cs` in each function project only wires that handler into
`Amazon.Lambda.RuntimeSupport`'s `LambdaBootstrapBuilder` and is not itself unit-testable; to exercise a
function end-to-end locally, build its Docker image and run it with the
[AWS Lambda Runtime Interface Emulator](https://github.com/aws/aws-lambda-runtime-interface-emulator) (`aws-lambda-rie`).

## Why container images, not the managed `dotnet` Lambda runtime

Every function deploys as a **Lambda container image** (`public.ecr.aws/lambda/provided:al2023` base,
running the self-contained published executable via `Amazon.Lambda.RuntimeSupport`), rather than the
AWS-managed `dotnetX` runtime. This keeps every function on .NET 10 - matching `apha-ptl-apps` - without
waiting on a managed runtime release, and reuses the same "build, validate, publish to ECR" CI/CD shape
already in place for the ECS apps.

**Lambda only supports Linux containers.** Unlike `apha-ptl-apps` (Windows containers on ECS), every
Dockerfile here targets Linux (`linux-x64`), and CI builds them on `ubuntu-latest` runners, not
`windows-2022`. Do not change a Lambda Dockerfile to a Windows base image - it will not deploy.

## Database connection

Every cleanup Lambda (`DeleteAttachments`, `DeleteConsultant`, `RemoveCustomerData`) connects to the same
PTLIMS SQL Server (RDS for SQL Server) as `PTL.Api`, using the identical pattern:
`Infrastructure`-equivalent `PTL.Lambda.Shared/SqlConnectionFactory` (Dapper on top of
`Microsoft.Data.SqlClient`), built from four separate config values rather than one connection string:

| Config key | Lambda environment variable |
|---|---|
| `Database:Host` | `Database__Host` |
| `Database:Name` | `Database__Name` |
| `Database:User` | `Database__User` |
| `Database:Password` | `Database__Password` |
| `Database:TrustServerCertificate` | `Database__TrustServerCertificate` (optional, default `false`) |

These four env var names are each function's contract with the platform team - the function's environment
variables should reference Secrets Manager (or SSM Parameter Store) ARNs for `Host`/`Name`/`User`/`Password`
via the Lambda function's configuration; nothing DB-related is ever baked into the image. Each
`Function.FunctionHandler` calls `StartupChecks.RequireDatabaseOptions` first and throws immediately if any
value is missing or blank - a clear, immediate invocation failure (visible in CloudWatch Logs) rather than a
silent misconfiguration that only surfaces on first DB use.

`EmailService` instead requires four `GraphApi:*` values (`GraphApi__TenantId`, `GraphApi__ClientId`,
`GraphApi__ClientSecret`, `GraphApi__SenderUserId`) for a Microsoft Graph app-only (client credentials) mail
send, validated the same way via `StartupChecks.RequireGraphApiOptions`.

## CI/CD

[`​.github/workflows/build-test-publish-images.yml`](.github/workflows/build-test-publish-images.yml)
mirrors `apha-ptl-apps`'s pipeline shape:

1. **`changes`** - `dorny/paths-filter` detects which of the 4 lambdas were touched by the PR/push (a
   change under `src/PTL.Lambda.Shared/**` affects all 4, since every function depends on it).
2. **`quality`** - restores, format-checks, builds (`--warnaserror`) and tests the whole solution.
3. **`sonarcloud`** *(optional, gated on the `SONAR_ENABLED` repo variable)*.
4. **`container-validation`** - builds only the affected lambdas' Docker images (matrix over the
   `changes` output), uploading each as a build artifact.
5. **`publish`** *(push to `main` / manual dispatch only)* - re-loads the validated image artifact (never
   rebuilds) and pushes it to the shared ECR repository as `<component>-sha-<12-char-sha>-<run-id>-<run-attempt>`,
   e.g. `delete-attachments-sha-abc123def456-42-1`.
6. **`gate`** - a single required status check that fails if any of the above didn't succeed.

Only lambdas whose code (or `PTL.Lambda.Shared`) changed are built and published on a given run - exactly
the same "only the changed component builds" behaviour as `apha-ptl-apps`.

### Required GitHub configuration

All 4 lambdas publish into **one shared ECR repository** (per the platform team), distinguished by image
tag prefix (`delete-attachments-...`, `delete-consultant-...`, `remove-customer-data-...`,
`email-service-...`):

| Name | Kind | Example value |
|---|---|---|
| `ECR_LAMBDA_REPOSITORY` | repo variable | `apha/ptl-lambda-1` |
| `EXPECTED_AWS_ACCOUNT_ID` | repo variable | 12-digit AWS account ID |
| `EXPECTED_AWS_REGION` | repo variable | `eu-west-2` |
| `SONAR_ENABLED` | repo variable | `true` / `false` |
| `SONAR_PROJECT_KEY` / `SONAR_ORGANIZATION` | repo variables | - |
| `AWS_ENV_REGION` | environment secret (`ecr-production`) | `eu-west-2` |
| `AWS_ENV_ACCOUNT` | environment secret (`ecr-production`) | 12-digit AWS account ID |
| `AWS_ENV_OIDC_ROLE` | environment secret (`ecr-production`) | OIDC deployment role name |
| `SONAR_TOKEN` | repo secret | - |

This repo only builds and publishes images to ECR - it does not deploy them to a Lambda function or
provision infrastructure (EventBridge rules, IAM roles, etc.); that is handled by the programme's separate
Terraform/CloudFormation pipeline, consistent with how `apha-ptl-apps` only publishes images for ECS to
pick up.
