# syntax=docker/dockerfile:1

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/PTL.Lambda.EmailService/PTL.Lambda.EmailService.csproj", "src/PTL.Lambda.EmailService/"]
COPY ["src/PTL.Lambda.Shared/PTL.Lambda.Shared.csproj", "src/PTL.Lambda.Shared/"]

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "src/PTL.Lambda.EmailService/PTL.Lambda.EmailService.csproj" \
    -r linux-x64 --self-contained true -p:PublishReadyToRun=true

COPY . .
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "src/PTL.Lambda.EmailService/PTL.Lambda.EmailService.csproj" -c Release -o /app/publish \
    --no-restore -r linux-x64 --self-contained true -p:PublishReadyToRun=true

# ---- Runtime stage: AWS Lambda custom runtime (Amazon Linux 2023) ----
# Lambda only supports Linux containers - unrelated to the ECS apps' Windows
# base images (apha-ptl-apps), and must not be changed to a Windows image.
FROM public.ecr.aws/lambda/provided:al2023 AS final
WORKDIR /var/task

COPY --from=build /app/publish .

ENTRYPOINT ["./PTL.Lambda.EmailService"]
