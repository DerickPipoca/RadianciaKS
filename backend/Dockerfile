FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY ["RadianciaKS.Api/RadianciaKS.Api.csproj", "RadianciaKS.Api/"]
COPY ["RadianciaKS.Application/RadianciaKS.Application.csproj", "RadianciaKS.Application/"]
COPY ["RadianciaKS.Domain/RadianciaKS.Domain.csproj", "RadianciaKS.Domain/"]
COPY ["RadianciaKS.Infrastructure/RadianciaKS.Infrastructure.csproj", "RadianciaKS.Infrastructure/"]
RUN dotnet restore "RadianciaKS.Api/RadianciaKS.Api.csproj"

COPY . .
WORKDIR "/src/RadianciaKS.Api"
RUN dotnet build "RadianciaKS.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RadianciaKS.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
EXPOSE 8080

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RadianciaKS.Api.dll"]