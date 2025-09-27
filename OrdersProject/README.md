# Orders System - Complete Observability Stack

A comprehensive .NET 8 microservices solution with full observability, structured logging, distributed tracing, and containerization.

## 🏗️ Architecture Overview

### Microservices

- **Orders.Api**: REST API for order management with full CRUD operations
- **OrdersProcessor.Service**: Background service for automated order status processing

### Observability Stack

- **Structured Logging**: Serilog with OpenTelemetry integration
- **Distributed Tracing**: OpenTelemetry → Jaeger
- **Metrics**: Prometheus + Grafana
- **Log Aggregation**: Loki/Promtail or ELK Stack
- **Correlation IDs**: End-to-end request tracking

### Infrastructure

- **Containerization**: Docker + Docker Compose
- **Database**: SQL Server
- **Load Balancing**: Ready for production scaling

## 🚀 Quick Start

### Prerequisites

- Docker Desktop
- .NET 8 SDK (for development)
- PowerShell (Windows) or Bash (Linux/Mac)

### Option 1: Using PowerShell (Windows)

```powershell
# Start the complete stack
.\manage-stack.ps1 start

# Generate sample traffic
.\manage-stack.ps1 traffic

# Check status
.\manage-stack.ps1 status

# View service URLs
.\manage-stack.ps1 urls
```

### Option 2: Using Bash (Linux/Mac/WSL)

```bash
# Make script executable
chmod +x manage-stack.sh

# Start the complete stack
./manage-stack.sh start

# Generate sample traffic
./manage-stack.sh traffic

# Check status
./manage-stack.sh status
```

### Option 3: Manual Docker Compose

```bash
# Start with Grafana/Loki stack
docker-compose up -d --build

# Or start with ELK stack
docker-compose --profile elk up -d --build
```

## 📊 Observability Features

### 1. Structured Logging with Serilog

- **JSON formatted logs** with consistent structure
- **Correlation ID tracking** across service boundaries
- **Multiple sinks**: Console, File, OpenTelemetry
- **Enrichers**: Machine name, process ID, thread ID, environment

### 2. Distributed Tracing with OpenTelemetry

- **Automatic instrumentation** for ASP.NET Core, Entity Framework, HTTP clients
- **Custom spans** for business operations
- **Trace correlation** with logs using TraceId
- **Export to Jaeger** for trace visualization

### 3. Metrics Collection

- **Application metrics**: Request rates, response times, error rates
- **Infrastructure metrics**: CPU, memory, database connections
- **Custom business metrics**: Orders created, processing times
- **Prometheus exposition** for metrics scraping

### 4. Correlation ID Implementation

- **Generated per request** with X-Correlation-ID header
- **Propagated across services** via HTTP headers
- **Logged in all operations** for request tracking
- **Trace correlation** linking logs and traces

## 🎯 Service Details

### Orders.Api

**Port**: 8080  
**Endpoints**:

- `GET /api/orders` - List orders with pagination and filtering
- `POST /api/orders` - Create new order
- `GET /api/orders/{id}` - Get specific order
- `PUT /api/orders/{id}` - Update order
- `PATCH /api/orders/{id}/status` - Update order status
- `DELETE /api/orders/{id}` - Cancel order
- `GET /api/orders/statistics` - Order analytics
- `GET /health` - Health check
- `GET /swagger` - API documentation

### OrdersProcessor.Service

**Port**: 8081  
**Background Processing**:

- **Pending → Processing**: After 2 minutes
- **Processing → Shipped**: After 10 minutes (sets ShippedDate)
- **Runs every**: 5 minutes (configurable)

**Endpoints**:

- `GET /api/orderprocessor/health` - Service health
- `GET /api/orderprocessor/statistics` - Order statistics
- `GET /api/orderprocessor/eligible-orders` - Orders ready for processing
- `POST /api/orderprocessor/process-now` - Manual trigger
- `GET /swagger` - API documentation

## 🔧 Configuration

### Serilog Configuration

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore.Database.Command": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j} {NewLine}{Exception}"
        }
      },
      {
        "Name": "OpenTelemetry",
        "Args": {
          "endpoint": "http://otel-collector:4317",
          "resourceAttributes": {
            "service.name": "orders-api"
          }
        }
      }
    ],
    "Enrich": [
      "FromLogContext",
      "WithMachineName",
      "WithProcessId",
      "WithThreadId",
      "WithEnvironmentName",
      "WithCorrelationId"
    ]
  }
}
```

### OpenTelemetry Configuration

```json
{
  "OpenTelemetry": {
    "ServiceName": "orders-api",
    "ServiceVersion": "1.0.0",
    "Exporters": {
      "OTLP": {
        "Endpoint": "http://otel-collector:4317"
      }
    }
  }
}
```

## 📈 Monitoring & Dashboards

### Service URLs

- **Grafana**: http://localhost:3000 (admin/admin123)
- **Prometheus**: http://localhost:9090
- **Jaeger**: http://localhost:16686
- **Orders API**: http://localhost:8080
- **Orders Processor**: http://localhost:8081

### Alternative ELK Stack URLs

- **Elasticsearch**: http://localhost:9200
- **Kibana**: http://localhost:5601

### Key Metrics to Monitor

1. **Request Rate**: Requests per second
2. **Error Rate**: 4xx/5xx responses percentage
3. **Response Time**: P95, P99 latencies
4. **Order Processing**: Orders by status, processing times
5. **Database Performance**: Query execution times
6. **System Health**: CPU, memory, disk usage

## 🔍 Correlation ID Tracking

### How it Works

1. **Request Initiation**: Client sends request with `X-Correlation-ID` header
2. **Auto-Generation**: If not provided, system generates unique ID
3. **Propagation**: ID flows through all services and operations
4. **Logging**: Every log entry includes the correlation ID
5. **Tracing**: Traces are linked via the same correlation ID

### Example Usage

```bash
# Create order with correlation ID
curl -X POST "http://localhost:8080/api/orders" \
     -H "Content-Type: application/json" \
     -H "X-Correlation-ID: order-12345-abc" \
     -d '{...}'

# Track the same request across services
# Logs will show: "CorrelationId": "order-12345-abc"
# Traces will be grouped by this ID
```

### Querying by Correlation ID

**In Grafana (Loki)**:

```
{service="orders-api"} |= "order-12345-abc"
```

**In Kibana**:

```
correlation_id:"order-12345-abc"
```

**In Jaeger**:
Search by Tag: `correlation_id=order-12345-abc`

## 🧪 Testing & Load Generation

### Generate Sample Traffic

```powershell
# PowerShell
.\manage-stack.ps1 traffic

# Bash
./manage-stack.sh traffic
```

### Continuous Load Testing

```powershell
# PowerShell (Press Ctrl+C to stop)
.\manage-stack.ps1 continuous-traffic

# Bash
./manage-stack.sh continuous-traffic
```

### Manual Testing

```bash
# Create order with correlation ID
correlation_id=$(uuidgen | tr '[:upper:]' '[:lower:]' | cut -c1-12)
curl -X POST "http://localhost:8080/api/orders" \
     -H "Content-Type: application/json" \
     -H "X-Correlation-ID: $correlation_id" \
     -d '{
       "customerName": "Test Customer",
       "customerEmail": "test@example.com",
       "shippingAddress": "123 Test St",
       "totalAmount": 99.99,
       "orderItems": [{
         "productName": "Test Product",
         "quantity": 1,
         "unitPrice": 99.99
       }]
     }'

# Track the order processing
echo "Track this correlation ID: $correlation_id"
```

## 📋 Common Workflows

### 1. Following an Order's Journey

1. **Create Order**: Use API with correlation ID
2. **View Logs**: Search for correlation ID in Grafana/Kibana
3. **Check Traces**: Find traces in Jaeger by correlation ID
4. **Monitor Processing**: Watch background service logs
5. **Verify Status**: Check order status changes

### 2. Debugging Issues

1. **Check Service Health**: Visit `/health` endpoints
2. **View Recent Logs**: Use log aggregation tools
3. **Analyze Traces**: Look for slow operations in Jaeger
4. **Check Metrics**: Monitor error rates in Grafana
5. **Database Performance**: Review EF Core traces

### 3. Performance Analysis

1. **Generate Load**: Use continuous traffic generation
2. **Monitor Dashboards**: Watch key metrics in Grafana
3. **Analyze Bottlenecks**: Use Jaeger trace analysis
4. **Review Logs**: Look for errors and warnings
5. **Database Analysis**: Monitor SQL query performance

## 🐳 Docker Stack Details

### Services in Stack

- **orders-api**: Main REST API
- **orders-processor**: Background processing service
- **sqlserver**: SQL Server database
- **otel-collector**: OpenTelemetry collector
- **jaeger**: Distributed tracing
- **loki**: Log aggregation
- **promtail**: Log shipping
- **prometheus**: Metrics collection
- **grafana**: Dashboards and visualization

### Alternative ELK Profile

- **elasticsearch**: Search and analytics
- **kibana**: Log visualization
- **logstash**: Log processing pipeline

### Volumes

- **sqlserver_data**: Database persistence
- **grafana_data**: Dashboard configurations
- **./logs**: Application log files

## 🔧 Development Setup

### Local Development

```bash
# Clone the repository
git clone <repository-url>
cd OrdersProject

# Restore packages
dotnet restore

# Run API locally
cd Orders.Api
dotnet run

# Run Processor locally (separate terminal)
cd OrdersProcessor.Service
dotnet run
```

### Environment Variables

```bash
# Database connection
ConnectionStrings__DefaultConnection="Server=localhost;Database=OrderDb;Trusted_Connection=true"

# OpenTelemetry
OpenTelemetry__ServiceName="orders-api"
OpenTelemetry__Exporters__OTLP__Endpoint="http://localhost:4317"

# Serilog
Serilog__WriteTo__0__Args__endpoint="http://localhost:4317"
```

## 🚀 Production Deployment

### Considerations

1. **Security**: Remove default passwords, use secrets management
2. **Persistence**: Configure proper volume mounts for data
3. **Scaling**: Use container orchestration (Kubernetes)
4. **Monitoring**: Set up alerts and notifications
5. **Backup**: Implement database backup strategies
6. **SSL/TLS**: Configure HTTPS endpoints
7. **Resource Limits**: Set appropriate CPU/memory limits

### Kubernetes Deployment (Future)

- Helm charts for easy deployment
- ConfigMaps for configuration management
- Secrets for sensitive data
- Ingress for external access
- HorizontalPodAutoscaler for scaling

## 📚 Additional Resources

### Documentation

- [OpenTelemetry .NET Guide](https://opentelemetry.io/docs/languages/net/)
- [Serilog Documentation](https://serilog.net/)
- [Grafana Documentation](https://grafana.com/docs/)
- [Prometheus Documentation](https://prometheus.io/docs/)
- [Jaeger Documentation](https://www.jaegertracing.io/docs/)

### Best Practices

- Always include correlation IDs in external API calls
- Use structured logging with consistent field names
- Monitor business metrics, not just technical metrics
- Set up alerts for error rates and response times
- Regularly review and clean up old logs and traces
- Test observability in development environment

This comprehensive setup provides enterprise-level observability for the Orders system, enabling effective monitoring, debugging, and performance optimization in both development and production environments.
