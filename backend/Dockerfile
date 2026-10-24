# Contexto de build: pasta backend/
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Erp.Domain/Erp.Domain.csproj src/Erp.Domain/
COPY src/Erp.Application/Erp.Application.csproj src/Erp.Application/
COPY src/Erp.Infrastructure/Erp.Infrastructure.csproj src/Erp.Infrastructure/
COPY src/Erp.Api/Erp.Api.csproj src/Erp.Api/
RUN dotnet restore src/Erp.Api/Erp.Api.csproj
COPY src/ src/
RUN dotnet publish src/Erp.Api/Erp.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
# Chaves do Data Protection (assinam os tokens de sessão), num volume para sobreviver a restarts.
RUN mkdir -p /data/keys && chown -R $APP_UID /data
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Erp.Api.dll"]
