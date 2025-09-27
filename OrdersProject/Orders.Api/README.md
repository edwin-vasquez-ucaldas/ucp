# Orders API - .NET 8 Web API

A comprehensive REST API for managing orders with full CRUD operations, built with .NET 8, Entity Framework Core, and SQL Server.

## Features

- **Complete CRUD Operations** for orders
- **Order Status Management** with automatic date tracking
- **Order Items Support** with detailed product information
- **Pagination** for efficient data retrieval
- **Filtering** by order status
- **Order Statistics** endpoint for reporting
- **Soft Delete** (cancellation) and hard delete options
- **Data Validation** with comprehensive error handling
- **Swagger/OpenAPI** documentation
- **Entity Framework Core** with SQL Server integration

## Order Status Values

The API supports the following order statuses:

1. **Pending** - Order received, awaiting processing
2. **Processing** - Order is being prepared
3. **Shipped** - Order has been shipped (sets ShippedDate automatically)
4. **Delivered** - Order has been delivered (sets DeliveredDate automatically)
5. **Cancelled** - Order has been cancelled
6. **Returned** - Order has been returned

## Database Setup

The API uses SQL Server LocalDB by default. The connection string in `appsettings.json` is:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=OrderDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

### Database Creation

The database will be automatically created when you first run the application. The `Program.cs` includes code to ensure the database exists:

```csharp
context.Database.EnsureCreated();
```

### Sample Data

The application includes seed data with:

- 2 sample orders
- Associated order items
- Different order statuses for testing

## API Endpoints

### Orders Controller (`/api/orders`)

#### GET `/api/orders`

- **Description**: Get all orders with optional filtering and pagination
- **Parameters**:
  - `status` (optional): Filter by OrderStatus (1-6)
  - `pageNumber` (optional): Page number (default: 1)
  - `pageSize` (optional): Page size (default: 10, max: 100)
- **Response**: List of `OrderSummaryDto` objects
- **Headers**: Includes pagination info (`X-Total-Count`, `X-Page-Number`, `X-Page-Size`)

#### GET `/api/orders/{id}`

- **Description**: Get a specific order by ID
- **Parameters**: `id` - Order ID
- **Response**: `OrderDto` object with full details including order items

#### POST `/api/orders`

- **Description**: Create a new order
- **Body**: `CreateOrderDto` object
- **Response**: Created `OrderDto` object
- **Status**: 201 Created with Location header

#### PUT `/api/orders/{id}`

- **Description**: Update an existing order
- **Parameters**: `id` - Order ID
- **Body**: `UpdateOrderDto` object (partial updates supported)
- **Response**: Updated `OrderDto` object

#### PATCH `/api/orders/{id}/status`

- **Description**: Update only the order status
- **Parameters**: `id` - Order ID
- **Body**: `OrderStatus` value (1-6)
- **Response**: Updated `OrderDto` object

#### DELETE `/api/orders/{id}`

- **Description**: Soft delete (cancel) an order
- **Parameters**: `id` - Order ID
- **Response**: 204 No Content
- **Note**: Sets status to Cancelled instead of removing from database

#### DELETE `/api/orders/{id}/hard`

- **Description**: Hard delete an order (permanent removal)
- **Parameters**: `id` - Order ID
- **Response**: 204 No Content
- **Restrictions**: Cannot delete delivered orders

#### GET `/api/orders/statistics`

- **Description**: Get order statistics and analytics
- **Response**: Object with total orders, revenue, and status breakdown

## Data Models

### Order

```csharp
public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; }
    public string CustomerEmail { get; set; }
    public string ShippingAddress { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    public string? Notes { get; set; }
    public List<OrderItem> OrderItems { get; set; }
}
```

### OrderItem

```csharp
public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string ProductName { get; set; }
    public string? ProductSku { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; } // Computed property
}
```

## Running the Application

1. **Restore packages**:

   ```bash
   dotnet restore
   ```

2. **Build the project**:

   ```bash
   dotnet build
   ```

3. **Run the application**:

   ```bash
   dotnet run
   ```

4. **Access the API**:
   - API Base URL: `https://localhost:7071/api`
   - Swagger UI: `https://localhost:7071` (in development mode)

## Testing with HTTP Files

The project includes `Orders.Api.http` with sample requests for all endpoints. You can use this file in Visual Studio or VS Code with the REST Client extension.

## Sample API Calls

### Create a New Order

```http
POST https://localhost:7071/api/orders
Content-Type: application/json

{
  "customerName": "Alice Johnson",
  "customerEmail": "alice.johnson@example.com",
  "shippingAddress": "789 Pine St, Newtown, NT 11111",
  "totalAmount": 299.99,
  "notes": "Rush order - please expedite",
  "orderItems": [
    {
      "productName": "Premium Widget",
      "productSku": "PWD-001",
      "quantity": 3,
      "unitPrice": 99.99
    }
  ]
}
```

### Update Order Status

```http
PATCH https://localhost:7071/api/orders/1/status
Content-Type: application/json

3
```

### Get Orders with Filtering

```http
GET https://localhost:7071/api/orders?status=2&pageNumber=1&pageSize=5
```

## Error Handling

The API includes comprehensive error handling:

- **400 Bad Request**: Invalid input data or validation errors
- **404 Not Found**: Order not found
- **500 Internal Server Error**: Server-side errors with logging

All errors include descriptive messages to help with debugging.

## Logging

The application uses built-in .NET logging with different log levels:

- **Information**: Normal operations (order creation, updates)
- **Error**: Exceptions and error conditions
- **Warning**: Potential issues

## Security Considerations

For production deployment, consider adding:

- **Authentication/Authorization** (JWT, OAuth2, etc.)
- **Rate Limiting** to prevent abuse
- **Input Sanitization** for additional security
- **HTTPS Enforcement**
- **Connection String Security** (Azure Key Vault, etc.)

## Database Migrations

For production use, consider using Entity Framework migrations instead of `EnsureCreated()`:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Technologies Used

- **.NET 8** - Framework
- **ASP.NET Core Web API** - Web framework
- **Entity Framework Core 8** - ORM
- **SQL Server** - Database
- **Swagger/OpenAPI** - API documentation
- **System.ComponentModel.DataAnnotations** - Validation
