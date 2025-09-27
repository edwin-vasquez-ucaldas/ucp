# SQL Database Setup for Orders API

This folder contains SQL scripts to set up the OrderDb database for the Orders.Api project.

## Files Overview

### 1. `01_CreateTables.sql`

- **Purpose**: Creates the complete database schema
- **Contains**:
  - Database creation (`OrderDb`)
  - Orders table with all columns and constraints
  - OrderItems table with foreign key relationships
  - Indexes for performance optimization
  - Views for common queries
  - Stored procedures for statistics

### 2. `02_SampleData.sql`

- **Purpose**: Populates tables with sample data for testing
- **Contains**:
  - 10 sample orders with various statuses
  - 15 sample order items
  - Data verification queries

### 3. `03_UsefulQueries.sql`

- **Purpose**: Collection of commonly used queries
- **Contains**:
  - Basic select queries
  - Filtering examples
  - Aggregation and statistics
  - Product analysis
  - Performance monitoring queries
  - Data validation checks

## Database Schema

### Orders Table Structure

```sql
Orders
├── Id (INT, IDENTITY, PRIMARY KEY)
├── CustomerName (NVARCHAR(100), NOT NULL)
├── CustomerEmail (NVARCHAR(255), NOT NULL)
├── ShippingAddress (NVARCHAR(500), NULL)
├── TotalAmount (DECIMAL(18,2), NOT NULL)
├── Status (INT, NOT NULL, DEFAULT 1)
├── OrderDate (DATETIME2, NOT NULL, DEFAULT GETUTCDATE())
├── ShippedDate (DATETIME2, NULL)
├── DeliveredDate (DATETIME2, NULL)
└── Notes (NVARCHAR(1000), NULL)
```

### OrderItems Table Structure

```sql
OrderItems
├── Id (INT, IDENTITY, PRIMARY KEY)
├── OrderId (INT, FOREIGN KEY -> Orders.Id)
├── ProductName (NVARCHAR(200), NOT NULL)
├── ProductSku (NVARCHAR(50), NULL)
├── Quantity (INT, NOT NULL)
└── UnitPrice (DECIMAL(18,2), NOT NULL)
```

### Order Status Values

- `1` - Pending
- `2` - Processing
- `3` - Shipped
- `4` - Delivered
- `5` - Cancelled
- `6` - Returned

## Setup Instructions

### Option 1: Using SQL Server Management Studio (SSMS)

1. Open SQL Server Management Studio
2. Connect to your SQL Server instance
3. Run scripts in order:
   - `01_CreateTables.sql`
   - `02_SampleData.sql`

### Option 2: Using sqlcmd Command Line

```bash
# Navigate to the SQL folder
cd "C:\Users\edwin\source\repos\OrdersProject\Orders.Api\SQL"

# Run the scripts
sqlcmd -S (localdb)\mssqllocaldb -i 01_CreateTables.sql
sqlcmd -S (localdb)\mssqllocaldb -i 02_SampleData.sql
```

### Option 3: Using Entity Framework (Recommended)

The .NET application will automatically create the database when you run it for the first time using Entity Framework migrations.

## Connection String

Update your `appsettings.json` with the appropriate connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=OrderDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

For SQL Server Express:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=OrderDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

For SQL Server with authentication:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=OrderDb;User Id=your-username;Password=your-password;MultipleActiveResultSets=true"
  }
}
```

## Performance Features

### Indexes Created

- `IX_Orders_Status` - For status filtering
- `IX_Orders_OrderDate` - For date-based queries
- `IX_Orders_CustomerEmail` - For customer lookups
- `IX_OrderItems_OrderId` - For order-item joins
- `IX_OrderItems_ProductSku` - For product lookups

### Views

- `vw_OrdersSummary` - Combines orders with calculated totals and item counts

### Stored Procedures

- `sp_GetOrderStatistics` - Returns comprehensive order statistics

## Data Validation

The tables include several constraints:

- Email format validation
- Positive amounts and quantities
- Status value ranges (1-6)
- Foreign key relationships with cascade delete

## Testing Queries

Use the queries in `03_UsefulQueries.sql` to:

- Verify data integrity
- Monitor performance
- Generate reports
- Analyze customer behavior
- Track product popularity

## Maintenance

Regular maintenance queries (add to scheduled jobs):

```sql
-- Update statistics for better performance
UPDATE STATISTICS [dbo].[Orders];
UPDATE STATISTICS [dbo].[OrderItems];

-- Check for fragmentation
SELECT
    object_name(object_id) as TableName,
    index_id,
    avg_fragmentation_in_percent
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED')
WHERE avg_fragmentation_in_percent > 30;
```
