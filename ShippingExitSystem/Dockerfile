# Etapa 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copiar csproj y restaurar dependencias
COPY *.csproj ./
RUN dotnet restore

# Copiar todo el código y compilar
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Etapa 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Instalar herramientas adicionales (para depuración)
RUN apt-get update && apt-get install -y curl

# Copiar la aplicación compilada
COPY --from=build /app/publish .

# Variables de entorno para Render
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:10000

# Puerto que Render espera
EXPOSE 10000

# Comando de inicio con logs detallados
ENTRYPOINT ["dotnet", "ShippingExitSystem.dll"]