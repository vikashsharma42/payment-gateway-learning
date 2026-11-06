/* ---------------------------------------------------------------
   Migration: audit columns, restaurant details, table location,
   and soft delete for tables and menu items.
   Run once against DineBook after the earlier migrations. Safe to re-run.
   --------------------------------------------------------------- */
USE DineBook;
GO

-- Restaurants: details and audit columns
IF COL_LENGTH('dbo.Restaurants', 'Phone') IS NULL
    ALTER TABLE dbo.Restaurants ADD Phone NVARCHAR(15) NOT NULL CONSTRAINT DF_Restaurants_Phone DEFAULT N'';
GO

IF COL_LENGTH('dbo.Restaurants', 'Description') IS NULL
    ALTER TABLE dbo.Restaurants ADD Description NVARCHAR(500) NOT NULL CONSTRAINT DF_Restaurants_Description DEFAULT N'';
GO

IF COL_LENGTH('dbo.Restaurants', 'Cuisine') IS NULL
    ALTER TABLE dbo.Restaurants ADD Cuisine NVARCHAR(50) NOT NULL CONSTRAINT DF_Restaurants_Cuisine DEFAULT N'';
GO

IF COL_LENGTH('dbo.Restaurants', 'CreatedAt') IS NULL
    ALTER TABLE dbo.Restaurants ADD CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_Restaurants_CreatedAt DEFAULT SYSUTCDATETIME();
GO

IF COL_LENGTH('dbo.Restaurants', 'UpdatedAt') IS NULL
    ALTER TABLE dbo.Restaurants ADD UpdatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_Restaurants_UpdatedAt DEFAULT SYSUTCDATETIME();
GO

-- Tables: location, soft delete, audit columns
IF COL_LENGTH('dbo.RestaurantTables', 'Location') IS NULL
    ALTER TABLE dbo.RestaurantTables ADD Location NVARCHAR(50) NOT NULL CONSTRAINT DF_RestaurantTables_Location DEFAULT N'';
GO

IF COL_LENGTH('dbo.RestaurantTables', 'IsDeleted') IS NULL
    ALTER TABLE dbo.RestaurantTables ADD IsDeleted BIT NOT NULL CONSTRAINT DF_RestaurantTables_IsDeleted DEFAULT 0;
GO

IF COL_LENGTH('dbo.RestaurantTables', 'DeletedAt') IS NULL
    ALTER TABLE dbo.RestaurantTables ADD DeletedAt DATETIMEOFFSET NULL;
GO

IF COL_LENGTH('dbo.RestaurantTables', 'CreatedAt') IS NULL
    ALTER TABLE dbo.RestaurantTables ADD CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_RestaurantTables_CreatedAt DEFAULT SYSUTCDATETIME();
GO

IF COL_LENGTH('dbo.RestaurantTables', 'UpdatedAt') IS NULL
    ALTER TABLE dbo.RestaurantTables ADD UpdatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_RestaurantTables_UpdatedAt DEFAULT SYSUTCDATETIME();
GO

-- A removed table keeps its label, so the unique label rule applies to active tables only
IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE name = 'IX_RestaurantTables_RestaurantId_Label' AND object_id = OBJECT_ID('dbo.RestaurantTables'))
    DROP INDEX IX_RestaurantTables_RestaurantId_Label ON dbo.RestaurantTables;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_RestaurantTables_RestaurantId_Label' AND object_id = OBJECT_ID('dbo.RestaurantTables'))
    CREATE UNIQUE INDEX IX_RestaurantTables_RestaurantId_Label
        ON dbo.RestaurantTables (RestaurantId, Label)
        WHERE IsDeleted = 0;
GO

-- Menu items: soft delete and audit columns
IF COL_LENGTH('dbo.MenuItems', 'IsDeleted') IS NULL
    ALTER TABLE dbo.MenuItems ADD IsDeleted BIT NOT NULL CONSTRAINT DF_MenuItems_IsDeleted DEFAULT 0;
GO

IF COL_LENGTH('dbo.MenuItems', 'DeletedAt') IS NULL
    ALTER TABLE dbo.MenuItems ADD DeletedAt DATETIMEOFFSET NULL;
GO

IF COL_LENGTH('dbo.MenuItems', 'CreatedAt') IS NULL
    ALTER TABLE dbo.MenuItems ADD CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_MenuItems_CreatedAt DEFAULT SYSUTCDATETIME();
GO

IF COL_LENGTH('dbo.MenuItems', 'UpdatedAt') IS NULL
    ALTER TABLE dbo.MenuItems ADD UpdatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_MenuItems_UpdatedAt DEFAULT SYSUTCDATETIME();
GO

-- Reservations, food orders and lines: audit columns
IF COL_LENGTH('dbo.Reservations', 'UpdatedAt') IS NULL
    ALTER TABLE dbo.Reservations ADD UpdatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_Reservations_UpdatedAt DEFAULT SYSUTCDATETIME();
GO

IF COL_LENGTH('dbo.FoodOrders', 'UpdatedAt') IS NULL
    ALTER TABLE dbo.FoodOrders ADD UpdatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_FoodOrders_UpdatedAt DEFAULT SYSUTCDATETIME();
GO

IF COL_LENGTH('dbo.FoodOrderLines', 'CreatedAt') IS NULL
    ALTER TABLE dbo.FoodOrderLines ADD CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_FoodOrderLines_CreatedAt DEFAULT SYSUTCDATETIME();
GO

IF COL_LENGTH('dbo.FoodOrderLines', 'UpdatedAt') IS NULL
    ALTER TABLE dbo.FoodOrderLines ADD UpdatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_FoodOrderLines_UpdatedAt DEFAULT SYSUTCDATETIME();
GO

-- Verify
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE COLUMN_NAME IN ('Phone', 'Description', 'Cuisine', 'Location', 'IsDeleted', 'DeletedAt', 'CreatedAt', 'UpdatedAt')
  AND TABLE_NAME IN ('Restaurants', 'RestaurantTables', 'MenuItems', 'Reservations', 'FoodOrders', 'FoodOrderLines')
ORDER BY TABLE_NAME, COLUMN_NAME;
GO
