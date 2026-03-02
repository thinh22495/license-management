FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY LicenseManagement.sln .
COPY src/LicenseManagement.Domain/LicenseManagement.Domain.csproj src/LicenseManagement.Domain/
COPY src/LicenseManagement.Application/LicenseManagement.Application.csproj src/LicenseManagement.Application/
COPY src/LicenseManagement.Infrastructure/LicenseManagement.Infrastructure.csproj src/LicenseManagement.Infrastructure/
COPY src/LicenseManagement.Api/LicenseManagement.Api.csproj src/LicenseManagement.Api/
COPY tests/LicenseManagement.UnitTests/LicenseManagement.UnitTests.csproj tests/LicenseManagement.UnitTests/
COPY tests/LicenseManagement.IntegrationTests/LicenseManagement.IntegrationTests.csproj tests/LicenseManagement.IntegrationTests/
RUN dotnet restore

# Copy source and build
COPY . .
RUN dotnet publish src/LicenseManagement.Api/LicenseManagement.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "LicenseManagement.Api.dll"]
