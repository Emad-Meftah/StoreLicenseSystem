FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY StoreLicense.Core/StoreLicense.Core.csproj StoreLicense.Core/
COPY StoreLicense.Infrastructure/StoreLicense.Infrastructure.csproj StoreLicense.Infrastructure/
COPY StoreLicense.Api/StoreLicense.Api.csproj StoreLicense.Api/

RUN dotnet restore StoreLicense.Api/StoreLicense.Api.csproj

COPY . .

WORKDIR /src/StoreLicense.Api

RUN dotnet publish StoreLicense.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 10000

ENTRYPOINT ["sh", "-c", "dotnet StoreLicense.Api.dll --urls http://0.0.0.0:${PORT:-10000}"]