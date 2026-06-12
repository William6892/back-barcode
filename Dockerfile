# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ShippingExitSystem/ShippingExitSystem.csproj ShippingExitSystem/
RUN dotnet restore ShippingExitSystem/ShippingExitSystem.csproj

# Copy everything else and build
COPY . .
WORKDIR /src/ShippingExitSystem
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Render uses PORT environment variable
ENV ASPNETCORE_URLS=http://+:${PORT:-10000}
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 10000
ENTRYPOINT ["dotnet", "ShippingExitSystem.dll"]
