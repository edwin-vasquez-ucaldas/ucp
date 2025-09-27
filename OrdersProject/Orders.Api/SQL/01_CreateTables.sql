-- =============================================
-- Orders Database Creation Script
-- Created for Orders.Api .NET 8 Project
-- Database: OrderDb
-- =============================================

-- Create Database (if not exists)
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'OrderDb')
BEGIN
    CREATE DATABASE [OrderDb]
    COLLATE SQL_Latin1_General_CP1_CI_AS;
END
GO

USE [OrderDb]
GO

-- =============================================
-- Create Orders Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Orders] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [CustomerName] NVARCHAR(100) NOT NULL,
        [CustomerEmail] NVARCHAR(255) NOT NULL,
        [ShippingAddress] NVARCHAR(500) NULL,
        [TotalAmount] DECIMAL(18,2) NOT NULL,
        [Status] INT NOT NULL DEFAULT 1, -- 1=Pending, 2=Processing, 3=Shipped, 4=Delivered, 5=Cancelled, 6=Returned
        [OrderDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [ShippedDate] DATETIME2 NULL,
        [DeliveredDate] DATETIME2 NULL,
        [Notes] NVARCHAR(1000) NULL,
        
        CONSTRAINT [PK_Orders] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [CK_Orders_Status] CHECK ([Status] BETWEEN 1 AND 6),
        CONSTRAINT [CK_Orders_TotalAmount] CHECK ([TotalAmount] >= 0),
        CONSTRAINT [CK_Orders_CustomerEmail] CHECK ([CustomerEmail] LIKE '%_@_%.__%')
    );
    
    PRINT 'Orders table created successfully';
END
ELSE
BEGIN
    PRINT 'Orders table already exists';
END
GO

-- =============================================
-- Create OrderItems Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OrderItems]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[OrderItems] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [OrderId] INT NOT NULL,
        [ProductName] NVARCHAR(200) NOT NULL,
        [ProductSku] NVARCHAR(50) NULL,
        [Quantity] INT NOT NULL,
        [UnitPrice] DECIMAL(18,2) NOT NULL,
        
        CONSTRAINT [PK_OrderItems] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_OrderItems_Orders] FOREIGN KEY ([OrderId]) 
            REFERENCES [dbo].[Orders]([Id]) ON DELETE CASCADE,
        CONSTRAINT [CK_OrderItems_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [CK_OrderItems_UnitPrice] CHECK ([UnitPrice] > 0)
    );
    
    PRINT 'OrderItems table created successfully';
END
ELSE
BEGIN
    PRINT 'OrderItems table already exists';
END
GO

-- =============================================
-- Create Indexes for Performance
-- =============================================

-- Index on Orders.Status for filtering by status
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Orders_Status')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Orders_Status] ON [dbo].[Orders] ([Status] ASC);
    PRINT 'Index IX_Orders_Status created';
END

-- Index on Orders.OrderDate for date-based queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Orders_OrderDate')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Orders_OrderDate] ON [dbo].[Orders] ([OrderDate] DESC);
    PRINT 'Index IX_Orders_OrderDate created';
END

-- Index on Orders.CustomerEmail for customer lookups
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Orders_CustomerEmail')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Orders_CustomerEmail] ON [dbo].[Orders] ([CustomerEmail] ASC);
    PRINT 'Index IX_Orders_CustomerEmail created';
END

-- Index on OrderItems.OrderId (foreign key, usually automatically indexed but being explicit)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OrderItems_OrderId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_OrderItems_OrderId] ON [dbo].[OrderItems] ([OrderId] ASC);
    PRINT 'Index IX_OrderItems_OrderId created';
END

-- Index on OrderItems.ProductSku for product lookups
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OrderItems_ProductSku')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_OrderItems_ProductSku] ON [dbo].[OrderItems] ([ProductSku] ASC);
    PRINT 'Index IX_OrderItems_ProductSku created';
END

-- =============================================
-- Create Views for Common Queries
-- =============================================

-- View: Orders with calculated totals and item counts
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_OrdersSummary')
    DROP VIEW [dbo].[vw_OrdersSummary];
GO

CREATE VIEW [dbo].[vw_OrdersSummary]
AS
SELECT 
    o.[Id],
    o.[CustomerName],
    o.[CustomerEmail],
    o.[ShippingAddress],
    o.[TotalAmount],
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
    o.[OrderDate],
    o.[ShippedDate],
    o.[DeliveredDate],
    o.[Notes],
    COUNT(oi.[Id]) AS [ItemCount],
    SUM(oi.[Quantity] * oi.[UnitPrice]) AS [CalculatedTotal]
FROM [dbo].[Orders] o
LEFT JOIN [dbo].[OrderItems] oi ON o.[Id] = oi.[OrderId]
GROUP BY 
    o.[Id], o.[CustomerName], o.[CustomerEmail], o.[ShippingAddress], 
    o.[TotalAmount], o.[Status], o.[OrderDate], o.[ShippedDate], 
    o.[DeliveredDate], o.[Notes];
GO

PRINT 'View vw_OrdersSummary created successfully';

-- =============================================
-- Create Stored Procedures
-- =============================================

-- Stored Procedure: Get Order Statistics
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetOrderStatistics')
    DROP PROCEDURE [dbo].[sp_GetOrderStatistics];
GO

CREATE PROCEDURE [dbo].[sp_GetOrderStatistics]
AS
BEGIN
    SET NOCOUNT ON;
    
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
        SUM([TotalAmount]) AS [TotalAmount],
        AVG([TotalAmount]) AS [AverageAmount]
    FROM [dbo].[Orders]
    GROUP BY [Status]
    ORDER BY [Status];
    
    -- Overall statistics
    SELECT 
        COUNT(*) AS [TotalOrders],
        SUM([TotalAmount]) AS [TotalRevenue],
        AVG([TotalAmount]) AS [AverageOrderValue],
        MIN([OrderDate]) AS [FirstOrderDate],
        MAX([OrderDate]) AS [LastOrderDate]
    FROM [dbo].[Orders];
END
GO

PRINT 'Stored procedure sp_GetOrderStatistics created successfully';

PRINT 'Database schema creation completed successfully!';