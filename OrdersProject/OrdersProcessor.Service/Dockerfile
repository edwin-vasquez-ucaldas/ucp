# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY OrdersProcessor.Service/OrdersProcessor.Service.csproj OrdersProcessor.Service/
RUN dotnet restore "OrdersProcessor.Service/OrdersProcessor.Service.csproj"

# Copy source code
COPY OrdersProcessor.Service/ OrdersProcessor.Service/

# Build the application
WORKDIR /src/OrdersProcessor.Service
RUN dotnet build "OrdersProcessor.Service.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "OrdersProcessor.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p logs

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:8081

# Expose port
EXPOSE 8081

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8081/health || exit 1

# Set entry point
ENTRYPOINT ["dotnet", "OrdersProcessor.Service.dll"]