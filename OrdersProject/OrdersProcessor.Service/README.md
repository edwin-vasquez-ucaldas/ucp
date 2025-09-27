# Orders Processor Service

A .NET 8 background service that automatically processes order status transitions based on configurable time intervals.

## Overview

This service runs in the background and performs the following operations:

1. **Pending → Processing**: Changes orders from `Pending` to `Processing` status after they've been created for more than 2 minutes
2. **Processing → Shipped**: Changes orders from `Processing` to `Shipped` status after they've been created for more than 10 minutes, and sets the `ShippedDate`

The service runs continuously with a configurable interval (default: every 5 minutes).

## Configuration

Configure the service behavior in `appsettings.json`:

```json
{
  "OrderProcessor": {
    "ProcessingIntervalMinutes": 5, // How often to check for orders to process
    "PendingToProcessingMinutes": 2, // Minutes to wait before Pending → Processing
    "ProcessingToShippedMinutes": 10 // Minutes to wait before Processing → Shipped
  }
}
```

## Background Service Features

### OrderStatusProcessorService

- **Runs continuously** in the background
- **Configurable intervals** for processing
- **Comprehensive logging** of all operations
- **Error handling** with retry logic
- **Statistics logging** after each processing cycle

### Processing Logic

1. **Identifies eligible orders** based on creation time and current status
2. **Updates status** and relevant dates
3. **Logs detailed information** about each processed order
4. **Saves changes** to database in batches
5. **Reports statistics** of current order status distribution

## API Endpoints

The service provides several API endpoints for monitoring and control:

### Health Check

```http
GET /api/orderprocessor/health
```

Returns service health status.

### Order Statistics

```http
GET /api/orderprocessor/statistics
```

Returns current order statistics grouped by status.

### Eligible Orders

```http
GET /api/orderprocessor/eligible-orders
```

Shows orders that are currently eligible for status updates.

### Manual Processing

```http
POST /api/orderprocessor/process-now
```

Manually triggers the order processing logic (useful for testing).

## Database Integration

- Uses the same `OrderDb` database as the Orders.Api project
- Shares the same Entity Framework models and context
- Operates on existing orders without disrupting the main API

## Logging

The service provides comprehensive logging at different levels:

- **Information**: Normal processing operations, statistics
- **Warning**: Non-critical issues, missing statistics
- **Error**: Database errors, processing failures

Example log entries:

```
[INFO] Order Status Processor Service started
[INFO] Processing order 123 from Pending to Processing. Order created 2023-09-24 10:00:00, 3.2 minutes ago
[INFO] Order processing cycle completed successfully. Updated 5 orders from Pending to Processing, 2 orders from Processing to Shipped
[INFO] Current order statistics: Pending: 10, Processing: 15, Shipped: 8, Delivered: 25
```

## Running the Service

### Prerequisites

- .NET 8 Runtime
- SQL Server (LocalDB, Express, or full version)
- Access to OrderDb database

### Startup

```bash
cd OrdersProcessor.Service
dotnet restore
dotnet build
dotnet run
```

The service will:

1. Start the background processing service
2. Launch the monitoring API on the configured port
3. Begin processing orders according to the configured schedule

### Development

- **Swagger UI**: Available at `/swagger` for API testing
- **Health endpoint**: Use `/api/orderprocessor/health` to verify service status
- **Manual trigger**: Use `/api/orderprocessor/process-now` for immediate testing

## Integration with Orders.Api

This service is designed to work alongside the main Orders.Api project:

1. **Orders.Api**: Handles CRUD operations, customer interactions
2. **OrdersProcessor.Service**: Handles automated background processing

Both services share the same database and can run simultaneously.

## Deployment Considerations

### Production Deployment

- Consider running as a Windows Service or Linux daemon
- Configure appropriate logging levels
- Set up monitoring alerts for processing failures
- Ensure database connection reliability

### Scaling

- Only run one instance per database to avoid conflicts
- Use distributed locking if multiple instances are needed
- Monitor processing performance and adjust intervals accordingly

### Monitoring

- Use the statistics endpoint for dashboards
- Monitor logs for processing errors
- Set up alerts for long-running operations

## Troubleshooting

### Common Issues

1. **Database Connection**: Verify connection string in appsettings.json
2. **No Orders Processing**: Check eligible-orders endpoint to verify orders meet criteria
3. **Service Not Starting**: Check logs for startup errors

### Debug Mode

Set logging level to `Debug` for detailed processing information:

```json
{
  "Logging": {
    "LogLevel": {
      "OrdersProcessor.Service": "Debug"
    }
  }
}
```

## Future Enhancements

Potential improvements:

- Add support for more complex business rules
- Implement notification system for status changes
- Add metrics collection and dashboards
- Support for holiday/weekend processing rules
- Integration with external shipping services
