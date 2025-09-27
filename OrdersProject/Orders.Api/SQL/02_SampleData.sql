-- =============================================
-- Sample Data Insertion Script
-- Orders Database (OrderDb)
-- =============================================

USE [OrderDb]
GO

-- =============================================
-- Insert Sample Orders
-- =============================================

-- Clear existing data (if any) for fresh start
DELETE FROM [dbo].[OrderItems];
DELETE FROM [dbo].[Orders];

-- Reset identity seeds
DBCC CHECKIDENT('[dbo].[Orders]', RESEED, 0);
DBCC CHECKIDENT('[dbo].[OrderItems]', RESEED, 0);

-- Insert sample orders
SET IDENTITY_INSERT [dbo].[Orders] ON;

INSERT INTO [dbo].[Orders] ([Id], [CustomerName], [CustomerEmail], [ShippingAddress], [TotalAmount], [Status], [OrderDate], [ShippedDate], [DeliveredDate], [Notes])
VALUES 
    (1, 'John Doe', 'john.doe@example.com', '123 Main St, Anytown, AT 12345', 99.99, 1, DATEADD(DAY, -1, GETUTCDATE()), NULL, NULL, 'First test order'),
    (2, 'Jane Smith', 'jane.smith@example.com', '456 Oak Ave, Somewhere, SW 67890', 149.99, 2, DATEADD(HOUR, -12, GETUTCDATE()), NULL, NULL, 'Priority order'),
    (3, 'Bob Johnson', 'bob.johnson@example.com', '789 Pine Rd, Elsewhere, EW 11111', 299.97, 3, DATEADD(DAY, -3, GETUTCDATE()), DATEADD(DAY, -1, GETUTCDATE()), NULL, 'Bulk order for office supplies'),
    (4, 'Alice Wilson', 'alice.wilson@example.com', '321 Elm St, Nowhere, NW 22222', 79.99, 4, DATEADD(DAY, -7, GETUTCDATE()), DATEADD(DAY, -5, GETUTCDATE()), DATEADD(DAY, -2, GETUTCDATE()), 'Express delivery completed'),
    (5, 'Charlie Brown', 'charlie.brown@example.com', '654 Maple Ave, Someplace, SP 33333', 199.50, 5, DATEADD(DAY, -2, GETUTCDATE()), NULL, NULL, 'Customer requested cancellation'),
    (6, 'Diana Prince', 'diana.prince@example.com', '987 Cedar St, Anywhere, AW 44444', 49.99, 1, DATEADD(HOUR, -6, GETUTCDATE()), NULL, NULL, 'Small order for personal use'),
    (7, 'Edward Norton', 'edward.norton@example.com', '147 Birch Blvd, Everywhere, EV 55555', 399.95, 2, DATEADD(HOUR, -3, GETUTCDATE()), NULL, NULL, 'Large order processing'),
    (8, 'Fiona Green', 'fiona.green@example.com', '258 Spruce Way, Wherever, WV 66666', 129.99, 3, DATEADD(DAY, -4, GETUTCDATE()), DATEADD(HOUR, -24, GETUTCDATE()), NULL, 'International shipping'),
    (9, 'George Miller', 'george.miller@example.com', '369 Willow Dr, Whenever, WN 77777', 89.99, 1, DATEADD(HOUR, -1, GETUTCDATE()), NULL, NULL, 'Repeat customer order'),
    (10, 'Helen Davis', 'helen.davis@example.com', '741 Aspen Ct, However, HW 88888', 249.97, 6, DATEADD(DAY, -10, GETUTCDATE()), DATEADD(DAY, -8, GETUTCDATE()), DATEADD(DAY, -6, GETUTCDATE()), 'Product returned due to defect');

SET IDENTITY_INSERT [dbo].[Orders] OFF;

-- =============================================
-- Insert Sample Order Items
-- =============================================

SET IDENTITY_INSERT [dbo].[OrderItems] ON;

INSERT INTO [dbo].[OrderItems] ([Id], [OrderId], [ProductName], [ProductSku], [Quantity], [UnitPrice])
VALUES 
    -- Order 1 items
    (1, 1, 'Widget A', 'WID-001', 2, 49.99),
    
    -- Order 2 items
    (2, 2, 'Gadget B', 'GAD-002', 1, 149.99),
    
    -- Order 3 items (bulk order)
    (3, 3, 'Office Chair', 'OFF-003', 1, 199.99),
    (4, 3, 'Desk Lamp', 'OFF-004', 2, 49.99),
    
    -- Order 4 items (delivered)
    (5, 4, 'Premium Headphones', 'AUD-005', 1, 79.99),
    
    -- Order 5 items (cancelled)
    (6, 5, 'Gaming Mouse', 'GAM-006', 1, 59.99),
    (7, 5, 'Keyboard', 'GAM-007', 1, 139.50),
    
    -- Order 6 items (small order)
    (8, 6, 'USB Cable', 'ACC-008', 1, 24.99),
    (9, 6, 'Phone Case', 'ACC-009', 1, 24.99),
    
    -- Order 7 items (large processing order)
    (10, 7, 'Monitor 24"', 'MON-010', 2, 199.99),
    
    -- Order 8 items (shipped international)
    (11, 8, 'Tablet Stand', 'ACC-011', 3, 43.33),
    
    -- Order 9 items (repeat customer)
    (12, 9, 'Widget A', 'WID-001', 1, 49.99),
    (13, 9, 'Extension Cord', 'ACC-012', 1, 39.99),
    
    -- Order 10 items (returned)
    (14, 10, 'Laptop Bag', 'BAG-013', 1, 89.99),
    (15, 10, 'Wireless Charger', 'CHG-014', 2, 79.99);

SET IDENTITY_INSERT [dbo].[OrderItems] OFF;

-- =============================================
-- Verify Data Insertion
-- =============================================

-- Show summary of inserted data
SELECT 
    'Orders' as TableName,
    COUNT(*) as RecordCount,
    SUM(TotalAmount) as TotalValue
FROM [dbo].[Orders]

UNION ALL

SELECT 
    'OrderItems' as TableName,
    COUNT(*) as RecordCount,
    SUM(Quantity * UnitPrice) as TotalValue
FROM [dbo].[OrderItems];

-- Show order status breakdown
SELECT 
    CASE [Status]
        WHEN 1 THEN 'Pending'
        WHEN 2 THEN 'Processing'
        WHEN 3 THEN 'Shipped'
        WHEN 4 THEN 'Delivered'
        WHEN 5 THEN 'Cancelled'
        WHEN 6 THEN 'Returned'
        ELSE 'Unknown'
    END AS StatusName,
    COUNT(*) as OrderCount,
    SUM(TotalAmount) as TotalAmount
FROM [dbo].[Orders]
GROUP BY [Status]
ORDER BY [Status];

-- Show detailed view using the summary view
SELECT TOP 5 * FROM [dbo].[vw_OrdersSummary] ORDER BY [OrderDate] DESC;

PRINT 'Sample data inserted successfully!';
PRINT 'Total Orders: 10';
PRINT 'Total Order Items: 15';
PRINT 'Ready for API testing!';