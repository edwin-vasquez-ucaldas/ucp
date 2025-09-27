-- =============================================
-- Useful SQL Queries for Orders Database
-- OrderDb - Common Operations
-- =============================================

USE [OrderDb]
GO

-- =============================================
-- Basic Select Queries
-- =============================================

-- Get all orders with status names
SELECT 
    o.[Id],
    o.[CustomerName],
    o.[CustomerEmail],
    o.[TotalAmount],
    CASE o.[Status]
        WHEN 1 THEN 'Pending'
        WHEN 2 THEN 'Processing'
        WHEN 3 THEN 'Shipped'
        WHEN 4 THEN 'Delivered'
        WHEN 5 THEN 'Cancelled'
        WHEN 6 THEN 'Returned'
        ELSE 'Unknown'
    END AS [StatusName],
    o.[OrderDate]
FROM [dbo].[Orders] o
ORDER BY o.[OrderDate] DESC;

-- Get orders with their items (detailed view)
SELECT 
    o.[Id] AS OrderId,
    o.[CustomerName],
    o.[TotalAmount],
    o.[Status],
    o.[OrderDate],
    oi.[Id] AS ItemId,
    oi.[ProductName],
    oi.[ProductSku],
    oi.[Quantity],
    oi.[UnitPrice],
    (oi.[Quantity] * oi.[UnitPrice]) AS ItemTotal
FROM [dbo].[Orders] o
INNER JOIN [dbo].[OrderItems] oi ON o.[Id] = oi.[OrderId]
ORDER BY o.[OrderDate] DESC, oi.[Id];

-- =============================================
-- Filtering Queries
-- =============================================

-- Get orders by status
SELECT * FROM [dbo].[Orders] 
WHERE [Status] = 1  -- Pending orders
ORDER BY [OrderDate] DESC;

-- Get orders from last 7 days
SELECT * FROM [dbo].[Orders]
WHERE [OrderDate] >= DATEADD(DAY, -7, GETUTCDATE())
ORDER BY [OrderDate] DESC;

-- Get orders by customer email
SELECT * FROM [dbo].[Orders]
WHERE [CustomerEmail] LIKE '%example.com%'
ORDER BY [OrderDate] DESC;

-- Get high-value orders (over $100)
SELECT * FROM [dbo].[Orders]
WHERE [TotalAmount] > 100
ORDER BY [TotalAmount] DESC;

-- =============================================
-- Aggregation Queries
-- =============================================

-- Order statistics by status
SELECT 
    [Status],
    CASE [Status]
        WHEN 1 THEN 'Pending'
        WHEN 2 THEN 'Processing'
        WHEN 3 THEN 'Shipped'
        WHEN 4 THEN 'Delivered'
        WHEN 5 THEN 'Cancelled'
        WHEN 6 THEN 'Returned'
        ELSE 'Unknown'
    END AS [StatusName],
    COUNT(*) AS [OrderCount],
    SUM([TotalAmount]) AS [TotalRevenue],
    AVG([TotalAmount]) AS [AverageOrderValue],
    MIN([TotalAmount]) AS [MinOrderValue],
    MAX([TotalAmount]) AS [MaxOrderValue]
FROM [dbo].[Orders]
GROUP BY [Status]
ORDER BY [Status];

-- Monthly order summary
SELECT 
    YEAR([OrderDate]) AS [Year],
    MONTH([OrderDate]) AS [Month],
    DATENAME(MONTH, [OrderDate]) AS [MonthName],
    COUNT(*) AS [OrderCount],
    SUM([TotalAmount]) AS [TotalRevenue],
    AVG([TotalAmount]) AS [AverageOrderValue]
FROM [dbo].[Orders]
GROUP BY YEAR([OrderDate]), MONTH([OrderDate]), DATENAME(MONTH, [OrderDate])
ORDER BY [Year] DESC, [Month] DESC;

-- Top customers by order value
SELECT 
    [CustomerName],
    [CustomerEmail],
    COUNT(*) AS [OrderCount],
    SUM([TotalAmount]) AS [TotalSpent],
    AVG([TotalAmount]) AS [AverageOrderValue],
    MAX([OrderDate]) AS [LastOrderDate]
FROM [dbo].[Orders]
GROUP BY [CustomerName], [CustomerEmail]
HAVING COUNT(*) > 0
ORDER BY SUM([TotalAmount]) DESC;

-- =============================================
-- Product Analysis Queries
-- =============================================

-- Most popular products
SELECT 
    oi.[ProductName],
    oi.[ProductSku],
    COUNT(*) AS [OrderCount],
    SUM(oi.[Quantity]) AS [TotalQuantitySold],
    AVG(oi.[UnitPrice]) AS [AveragePrice],
    SUM(oi.[Quantity] * oi.[UnitPrice]) AS [TotalRevenue]
FROM [dbo].[OrderItems] oi
INNER JOIN [dbo].[Orders] o ON oi.[OrderId] = o.[Id]
WHERE o.[Status] NOT IN (5, 6) -- Exclude cancelled and returned orders
GROUP BY oi.[ProductName], oi.[ProductSku]
ORDER BY SUM(oi.[Quantity]) DESC;

-- Product performance by order status
SELECT 
    oi.[ProductName],
    o.[Status],
    CASE o.[Status]
        WHEN 1 THEN 'Pending'
        WHEN 2 THEN 'Processing'
        WHEN 3 THEN 'Shipped'
        WHEN 4 THEN 'Delivered'
        WHEN 5 THEN 'Cancelled'
        WHEN 6 THEN 'Returned'
        ELSE 'Unknown'
    END AS [StatusName],
    COUNT(*) AS [OrderCount],
    SUM(oi.[Quantity]) AS [TotalQuantity],
    SUM(oi.[Quantity] * oi.[UnitPrice]) AS [TotalValue]
FROM [dbo].[OrderItems] oi
INNER JOIN [dbo].[Orders] o ON oi.[OrderId] = o.[Id]
GROUP BY oi.[ProductName], o.[Status]
ORDER BY oi.[ProductName], o.[Status];

-- =============================================
-- Performance and Operational Queries
-- =============================================

-- Orders processing time analysis
SELECT 
    [Id],
    [CustomerName],
    [Status],
    [OrderDate],
    [ShippedDate],
    [DeliveredDate],
    CASE 
        WHEN [ShippedDate] IS NOT NULL 
        THEN DATEDIFF(HOUR, [OrderDate], [ShippedDate])
        ELSE NULL 
    END AS [HoursToShip],
    CASE 
        WHEN [DeliveredDate] IS NOT NULL 
        THEN DATEDIFF(HOUR, [OrderDate], [DeliveredDate])
        ELSE NULL 
    END AS [HoursToDeliver],
    CASE 
        WHEN [DeliveredDate] IS NOT NULL AND [ShippedDate] IS NOT NULL
        THEN DATEDIFF(HOUR, [ShippedDate], [DeliveredDate])
        ELSE NULL 
    END AS [ShippingTime]
FROM [dbo].[Orders]
WHERE [Status] IN (3, 4) -- Shipped or Delivered
ORDER BY [OrderDate] DESC;

-- Average processing times
SELECT 
    AVG(CASE WHEN [ShippedDate] IS NOT NULL 
        THEN DATEDIFF(HOUR, [OrderDate], [ShippedDate]) END) AS [AvgHoursToShip],
    AVG(CASE WHEN [DeliveredDate] IS NOT NULL 
        THEN DATEDIFF(HOUR, [OrderDate], [DeliveredDate]) END) AS [AvgHoursToDeliver],
    AVG(CASE WHEN [DeliveredDate] IS NOT NULL AND [ShippedDate] IS NOT NULL
        THEN DATEDIFF(HOUR, [ShippedDate], [DeliveredDate]) END) AS [AvgShippingTime]
FROM [dbo].[Orders];

-- =============================================
-- Data Validation Queries
-- =============================================

-- Check for orders with mismatched totals
SELECT 
    o.[Id],
    o.[CustomerName],
    o.[TotalAmount] AS [StoredTotal],
    SUM(oi.[Quantity] * oi.[UnitPrice]) AS [CalculatedTotal],
    ABS(o.[TotalAmount] - SUM(oi.[Quantity] * oi.[UnitPrice])) AS [Difference]
FROM [dbo].[Orders] o
LEFT JOIN [dbo].[OrderItems] oi ON o.[Id] = oi.[OrderId]
GROUP BY o.[Id], o.[CustomerName], o.[TotalAmount]
HAVING ABS(o.[TotalAmount] - ISNULL(SUM(oi.[Quantity] * oi.[UnitPrice]), 0)) > 0.01
ORDER BY [Difference] DESC;

-- Find orders without items
SELECT o.* FROM [dbo].[Orders] o
LEFT JOIN [dbo].[OrderItems] oi ON o.[Id] = oi.[OrderId]
WHERE oi.[OrderId] IS NULL;

-- Check for invalid email formats (basic check)
SELECT [Id], [CustomerName], [CustomerEmail]
FROM [dbo].[Orders]
WHERE [CustomerEmail] NOT LIKE '%_@_%.__%'
   OR [CustomerEmail] IS NULL
   OR [CustomerEmail] = '';

-- =============================================
-- Using the Summary View
-- =============================================

-- Quick overview using the summary view
SELECT * FROM [dbo].[vw_OrdersSummary]
ORDER BY [OrderDate] DESC;

-- Summary view with filtering
SELECT * FROM [dbo].[vw_OrdersSummary]
WHERE [Status] = 1 AND [TotalAmount] > 50
ORDER BY [TotalAmount] DESC;