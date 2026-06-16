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

ENV ASPNETCORE_ENVIRONMENT=Production

# Render asigna el puerto via $PORT (default 10000)
EXPOSE 10000
CMD ["sh", "-c", "dotnet ShippingExitSystem.dll --urls http://+:${PORT:-10000}"]
