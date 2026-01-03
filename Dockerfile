# Multi-stage Dockerfile for .NET 8 API
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["GhanaHybridRentalApi.csproj", "./"]
RUN dotnet restore "GhanaHybridRentalApi.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "GhanaHybridRentalApi.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "GhanaHybridRentalApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create a non-root user for security
RUN addgroup --system --gid 1000 appuser && \
    adduser --system --uid 1000 --gid 1000 --shell /bin/sh appuser

# Copy published application
COPY --from=publish /app/publish .

# Create uploads directory
RUN mkdir -p /app/uploads

# Note: Running as root to bind to port 80 (required for Cloudflare proxy)
# Expose port (Azure will configure this dynamically)
EXPOSE 80

# Set environment variable for ASP.NET Core to listen on port 80
ENV ASPNETCORE_URLS=http://+:80

# Entry point
ENTRYPOINT ["dotnet", "GhanaHybridRentalApi.dll"]
