/* ---------------------------------------------------------------
   Migration: DDD domain update.
   - Adds Users, UserRefreshTokens, Customers, MenuCategories, Bills.
   - Restaurants.Status and RestaurantTables.Status (replaces RestaurantTables.IsActive).
   - MenuItems.MenuCategoryId and Reservations.CustomerId, filled from the existing rows.
   - The old product Orders table becomes the new Payments table (one payment per bill).
     Old product orders cannot belong to a bill, so they and Products are kept as
     LegacyOrders and LegacyProducts. The application no longer reads them.
   - FoodOrders -> Orders and FoodOrderLines -> OrderItems (FoodOrderId -> OrderId).
   - Audit columns CreatedDateTime, CreatedBy, UpdatedDateTime, UpdatedBy on every entity table
     (CreatedAt / UpdatedAt are renamed, so existing values are kept).
   Run once against DineBook after the earlier migrations. Safe to re-run.
   Take a backup first.
   --------------------------------------------------------------- */
USE DineBook;
GO

SET XACT_ABORT ON;
GO

/* 1. Sign-in tables ------------------------------------------------ */
IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        Id           BIGINT IDENTITY(1, 1) NOT NULL,
        Email        NVARCHAR(254) NOT NULL,
        PasswordHash NVARCHAR(500) NOT NULL,
        FirstName    NVARCHAR(100) NOT NULL,
        LastName     NVARCHAR(100) NOT NULL,
        Role         NVARCHAR(20) NOT NULL,
        IsActive     BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
        CreatedDateTime    DATETIMEOFFSET NOT NULL CONSTRAINT DF_Users_CreatedDateTime DEFAULT SYSUTCDATETIME(),
        UpdatedDateTime    DATETIMEOFFSET NOT NULL CONSTRAINT DF_Users_UpdatedDateTime DEFAULT SYSUTCDATETIME(),
        CreatedBy         NVARCHAR(256) NULL,
        UpdatedBy         NVARCHAR(256) NULL,
        CONSTRAINT PK_Users PRIMARY KEY (Id),
        CONSTRAINT UQ_Users_Email UNIQUE (Email),
        CONSTRAINT CK_Users_Role CHECK (Role IN ('Customer', 'Staff', 'Manager', 'Admin'))
    );
END
GO

IF OBJECT_ID('dbo.UserRefreshTokens', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserRefreshTokens (
        Id                  BIGINT IDENTITY(1, 1) NOT NULL,
        TokenHash           NVARCHAR(64) NOT NULL,
        ApplicationUserId   BIGINT NOT NULL,
        CreatedAtUtc        DATETIMEOFFSET NOT NULL,
        ExpiresAtUtc        DATETIMEOFFSET NOT NULL,
        RevokedAtUtc        DATETIMEOFFSET NULL,
        RevokeReason        NVARCHAR(20) NULL,
        ReplacedByTokenHash NVARCHAR(64) NULL,
        CONSTRAINT PK_UserRefreshTokens PRIMARY KEY (Id),
        CONSTRAINT UQ_UserRefreshTokens_TokenHash UNIQUE (TokenHash),
        CONSTRAINT FK_UserRefreshTokens_Users FOREIGN KEY (ApplicationUserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE,
        CONSTRAINT CK_UserRefreshTokens_RevokeReason CHECK (
            RevokeReason IS NULL
            OR RevokeReason IN ('Rotated', 'LoggedOut', 'SessionReplaced', 'AdminRevoked', 'ReuseDetected'))
    );

    CREATE NONCLUSTERED INDEX IX_UserRefreshTokens_ApplicationUserId ON dbo.UserRefreshTokens (ApplicationUserId);
END
GO

/* 2. Customers, filled from existing bookings ---------------------- */
IF OBJECT_ID('dbo.Customers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers (
        Id                BIGINT IDENTITY(1, 1) NOT NULL,
        Name              NVARCHAR(100) NOT NULL,
        Phone             NVARCHAR(15) NOT NULL,
        Email             NVARCHAR(254) NOT NULL,
        ApplicationUserId BIGINT NULL,
        CreatedDateTime         DATETIMEOFFSET NOT NULL CONSTRAINT DF_Customers_CreatedDateTime DEFAULT SYSUTCDATETIME(),
        UpdatedDateTime         DATETIMEOFFSET NOT NULL CONSTRAINT DF_Customers_UpdatedDateTime DEFAULT SYSUTCDATETIME(),
        CreatedBy         NVARCHAR(256) NULL,
        UpdatedBy         NVARCHAR(256) NULL,
        CONSTRAINT PK_Customers PRIMARY KEY (Id),
        CONSTRAINT UQ_Customers_Phone UNIQUE (Phone),
        CONSTRAINT FK_Customers_Users FOREIGN KEY (ApplicationUserId) REFERENCES dbo.Users (Id)
    );
END
GO

IF COL_LENGTH('dbo.Reservations', 'CustomerId') IS NULL
    ALTER TABLE dbo.Reservations ADD CustomerId BIGINT NULL;
GO

-- One customer per phone number, with the name and email from the newest booking
INSERT INTO dbo.Customers (Name, Phone, Email)
SELECT latest.CustomerName, latest.CustomerPhone, latest.CustomerEmail
FROM (
    SELECT CustomerName, CustomerPhone, CustomerEmail,
           ROW_NUMBER() OVER (PARTITION BY CustomerPhone ORDER BY Id DESC) AS Newest
    FROM dbo.Reservations
) AS latest
WHERE latest.Newest = 1
  AND NOT EXISTS (SELECT 1 FROM dbo.Customers c WHERE c.Phone = latest.CustomerPhone);
GO

UPDATE r
SET r.CustomerId = c.Id
FROM dbo.Reservations r
JOIN dbo.Customers c ON c.Phone = r.CustomerPhone
WHERE r.CustomerId IS NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Reservations_Customers')
    ALTER TABLE dbo.Reservations
        ADD CONSTRAINT FK_Reservations_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reservations_CustomerId' AND object_id = OBJECT_ID('dbo.Reservations'))
    CREATE NONCLUSTERED INDEX IX_Reservations_CustomerId ON dbo.Reservations (CustomerId);
GO

/* 3. Restaurant and table status ----------------------------------- */
IF COL_LENGTH('dbo.Restaurants', 'Status') IS NULL
    ALTER TABLE dbo.Restaurants
        ADD Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Restaurants_Status DEFAULT N'Active';
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Restaurants_Status')
    ALTER TABLE dbo.Restaurants
        ADD CONSTRAINT CK_Restaurants_Status CHECK (Status IN ('Active', 'Inactive'));
GO

IF COL_LENGTH('dbo.RestaurantTables', 'Status') IS NULL
    ALTER TABLE dbo.RestaurantTables
        ADD Status NVARCHAR(20) NOT NULL CONSTRAINT DF_RestaurantTables_Status DEFAULT N'Available';
GO

-- IsActive = 0 becomes OutOfService, then the old column goes away
IF COL_LENGTH('dbo.RestaurantTables', 'IsActive') IS NOT NULL
BEGIN
    EXEC (N'UPDATE dbo.RestaurantTables SET Status = N''OutOfService'' WHERE IsActive = 0;');

    DECLARE @df sysname = (
        SELECT dc.name
        FROM sys.default_constraints dc
        JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
        WHERE dc.parent_object_id = OBJECT_ID('dbo.RestaurantTables') AND c.name = 'IsActive');

    IF @df IS NOT NULL
        EXEC (N'ALTER TABLE dbo.RestaurantTables DROP CONSTRAINT ' + @df);

    ALTER TABLE dbo.RestaurantTables DROP COLUMN IsActive;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_RestaurantTables_Status')
    ALTER TABLE dbo.RestaurantTables
        ADD CONSTRAINT CK_RestaurantTables_Status CHECK (Status IN ('Available', 'Occupied', 'OutOfService'));
GO

/* 4. Menu categories, filled from the category text on dishes ------ */
IF OBJECT_ID('dbo.MenuCategories', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MenuCategories (
        Id           BIGINT IDENTITY(1, 1) NOT NULL,
        RestaurantId BIGINT NOT NULL,
        Name         NVARCHAR(50) NOT NULL,
        CreatedDateTime    DATETIMEOFFSET NOT NULL CONSTRAINT DF_MenuCategories_CreatedDateTime DEFAULT SYSUTCDATETIME(),
        UpdatedDateTime    DATETIMEOFFSET NOT NULL CONSTRAINT DF_MenuCategories_UpdatedDateTime DEFAULT SYSUTCDATETIME(),
        CreatedBy         NVARCHAR(256) NULL,
        UpdatedBy         NVARCHAR(256) NULL,
        CONSTRAINT PK_MenuCategories PRIMARY KEY (Id),
        CONSTRAINT FK_MenuCategories_Restaurants FOREIGN KEY (RestaurantId) REFERENCES dbo.Restaurants (Id) ON DELETE CASCADE,
        CONSTRAINT UQ_MenuCategories_RestaurantId_Name UNIQUE (RestaurantId, Name)
    );
END
GO

IF COL_LENGTH('dbo.MenuItems', 'MenuCategoryId') IS NULL
    ALTER TABLE dbo.MenuItems ADD MenuCategoryId BIGINT NULL;
GO

INSERT INTO dbo.MenuCategories (RestaurantId, Name)
SELECT DISTINCT m.RestaurantId, m.Category
FROM dbo.MenuItems m
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.MenuCategories c WHERE c.RestaurantId = m.RestaurantId AND c.Name = m.Category);
GO

UPDATE m
SET m.MenuCategoryId = c.Id
FROM dbo.MenuItems m
JOIN dbo.MenuCategories c ON c.RestaurantId = m.RestaurantId AND c.Name = m.Category
WHERE m.MenuCategoryId IS NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.MenuItems') AND name = 'MenuCategoryId' AND is_nullable = 1)
    ALTER TABLE dbo.MenuItems ALTER COLUMN MenuCategoryId BIGINT NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MenuItems_MenuCategories')
    ALTER TABLE dbo.MenuItems
        ADD CONSTRAINT FK_MenuItems_MenuCategories FOREIGN KEY (MenuCategoryId) REFERENCES dbo.MenuCategories (Id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MenuItems_MenuCategoryId' AND object_id = OBJECT_ID('dbo.MenuItems'))
    CREATE NONCLUSTERED INDEX IX_MenuItems_MenuCategoryId ON dbo.MenuItems (MenuCategoryId);
GO

/* 5. Old product orders -> LegacyOrders, Products -> LegacyProducts - */
-- The old Orders table is recognised by its ProductId column. The new Orders table has none.
IF COL_LENGTH('dbo.Orders', 'ProductId') IS NOT NULL
BEGIN
    EXEC sp_rename 'dbo.Orders', 'LegacyOrders';
END
GO

IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL AND OBJECT_ID('dbo.LegacyProducts', 'U') IS NULL
BEGIN
    EXEC sp_rename 'dbo.Products', 'LegacyProducts';
END
GO

/* 6. FoodOrders -> Orders, FoodOrderLines -> OrderItems ------------ */
IF OBJECT_ID('dbo.FoodOrders', 'U') IS NOT NULL AND OBJECT_ID('dbo.Orders', 'U') IS NULL
BEGIN
    EXEC sp_rename 'dbo.FoodOrders', 'Orders';
END
GO

IF OBJECT_ID('dbo.FoodOrderLines', 'U') IS NOT NULL AND OBJECT_ID('dbo.OrderItems', 'U') IS NULL
BEGIN
    EXEC sp_rename 'dbo.FoodOrderLines', 'OrderItems';
END
GO

IF COL_LENGTH('dbo.OrderItems', 'FoodOrderId') IS NOT NULL
BEGIN
    EXEC sp_rename 'dbo.OrderItems.FoodOrderId', 'OrderId', 'COLUMN';
END
GO

/* 7. Bills and Payments -------------------------------------------- */
IF OBJECT_ID('dbo.Bills', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Bills (
        Id            BIGINT IDENTITY(1, 1) NOT NULL,
        ReservationId BIGINT NOT NULL,
        RestaurantId  BIGINT NOT NULL,
        Amount        DECIMAL(18, 2) NOT NULL,
        Status        NVARCHAR(20) NOT NULL,
        CreatedDateTime     DATETIMEOFFSET NOT NULL,
        UpdatedDateTime     DATETIMEOFFSET NOT NULL CONSTRAINT DF_Bills_UpdatedDateTime DEFAULT SYSUTCDATETIME(),
        CreatedBy         NVARCHAR(256) NULL,
        UpdatedBy         NVARCHAR(256) NULL,
        CONSTRAINT PK_Bills PRIMARY KEY (Id),
        CONSTRAINT FK_Bills_Reservations FOREIGN KEY (ReservationId) REFERENCES dbo.Reservations (Id),
        CONSTRAINT FK_Bills_Restaurants FOREIGN KEY (RestaurantId) REFERENCES dbo.Restaurants (Id),
        CONSTRAINT CK_Bills_Amount CHECK (Amount > 0),
        CONSTRAINT CK_Bills_Status CHECK (Status IN ('Pending', 'Paid', 'Failed', 'Refunded'))
    );

    CREATE NONCLUSTERED INDEX IX_Bills_ReservationId ON dbo.Bills (ReservationId);
END
GO

IF OBJECT_ID('dbo.Payments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Payments (
        Id               BIGINT IDENTITY(1, 1) NOT NULL,
        BillId           BIGINT NOT NULL,
        PublicId         UNIQUEIDENTIFIER NOT NULL,
        Amount           DECIMAL(18, 2) NOT NULL,
        GatewayOrderId   NVARCHAR(100) NOT NULL,
        PaymentSessionId NVARCHAR(200) NULL,
        IdempotencyKey   NVARCHAR(64) NULL,
        CustomerEmail    NVARCHAR(254) NULL,
        RefundId         NVARCHAR(64) NULL,
        Status           NVARCHAR(20) NOT NULL,
        CreatedDateTime DATETIMEOFFSET NOT NULL CONSTRAINT DF_Payments_CreatedDateTime DEFAULT SYSUTCDATETIME(),
        CreatedBy        NVARCHAR(256) NULL,
        UpdatedDateTime DATETIMEOFFSET NOT NULL CONSTRAINT DF_Payments_UpdatedDateTime DEFAULT SYSUTCDATETIME(),
        UpdatedBy        NVARCHAR(256) NULL,
        CONSTRAINT PK_Payments PRIMARY KEY (Id),
        CONSTRAINT FK_Payments_Bills FOREIGN KEY (BillId) REFERENCES dbo.Bills (Id),
        CONSTRAINT UQ_Payments_GatewayOrderId UNIQUE (GatewayOrderId),
        CONSTRAINT UQ_Payments_PublicId UNIQUE (PublicId),
        CONSTRAINT CK_Payments_Status CHECK (Status IN ('Pending', 'Paid', 'Failed', 'Refunded'))
    );

    CREATE NONCLUSTERED INDEX IX_Payments_BillId ON dbo.Payments (BillId);

    CREATE UNIQUE NONCLUSTERED INDEX UX_Payments_IdempotencyKey
        ON dbo.Payments (IdempotencyKey)
        WHERE IdempotencyKey IS NOT NULL;
END
GO

/* 8. Audit columns -------------------------------------------------
   Every table whose entity implements IAuditableEntity gets
   CreatedDateTime, CreatedBy, UpdatedDateTime and UpdatedBy.
   Old CreatedAt / UpdatedAt columns are renamed, so existing values are kept.
   --------------------------------------------------------------- */
DECLARE @auditTables TABLE (TableName sysname NOT NULL);
INSERT INTO @auditTables (TableName)
VALUES (N'Users'), (N'Customers'), (N'Restaurants'), (N'RestaurantTables'), (N'Reservations'),
       (N'MenuCategories'), (N'MenuItems'), (N'Orders'), (N'OrderItems'), (N'Bills'), (N'Payments');

DECLARE @table sysname, @object nvarchar(300), @sql nvarchar(max);
DECLARE auditCursor CURSOR LOCAL FAST_FORWARD FOR SELECT TableName FROM @auditTables;
OPEN auditCursor;
FETCH NEXT FROM auditCursor INTO @table;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @object = N'dbo.' + @table;

    IF OBJECT_ID(@object, 'U') IS NOT NULL
    BEGIN
        -- CreatedAt -> CreatedDateTime
        IF COL_LENGTH(@object, 'CreatedAt') IS NOT NULL AND COL_LENGTH(@object, 'CreatedDateTime') IS NULL
        BEGIN
            SET @sql = @object + N'.CreatedAt';
            EXEC sp_rename @sql, N'CreatedDateTime', N'COLUMN';
        END

        -- UpdatedAt -> UpdatedDateTime
        IF COL_LENGTH(@object, 'UpdatedAt') IS NOT NULL AND COL_LENGTH(@object, 'UpdatedDateTime') IS NULL
        BEGIN
            SET @sql = @object + N'.UpdatedAt';
            EXEC sp_rename @sql, N'UpdatedDateTime', N'COLUMN';
        END

        IF COL_LENGTH(@object, 'CreatedDateTime') IS NULL
        BEGIN
            SET @sql = N'ALTER TABLE ' + @object + N' ADD CreatedDateTime DATETIMEOFFSET NOT NULL CONSTRAINT DF_'
                     + @table + N'_CreatedDateTime DEFAULT SYSUTCDATETIME();';
            EXEC (@sql);
        END

        IF COL_LENGTH(@object, 'UpdatedDateTime') IS NULL
        BEGIN
            SET @sql = N'ALTER TABLE ' + @object + N' ADD UpdatedDateTime DATETIMEOFFSET NOT NULL CONSTRAINT DF_'
                     + @table + N'_UpdatedDateTime DEFAULT SYSUTCDATETIME();';
            EXEC (@sql);
        END

        IF COL_LENGTH(@object, 'CreatedBy') IS NULL
        BEGIN
            SET @sql = N'ALTER TABLE ' + @object + N' ADD CreatedBy NVARCHAR(256) NULL;';
            EXEC (@sql);
        END

        IF COL_LENGTH(@object, 'UpdatedBy') IS NULL
        BEGIN
            SET @sql = N'ALTER TABLE ' + @object + N' ADD UpdatedBy NVARCHAR(256) NULL;';
            EXEC (@sql);
        END
    END

    FETCH NEXT FROM auditCursor INTO @table;
END

CLOSE auditCursor;
DEALLOCATE auditCursor;
GO

/* Check ------------------------------------------------------------ */
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME IN ('Users', 'UserRefreshTokens', 'Customers', 'Restaurants', 'RestaurantTables', 'Reservations',
                     'MenuCategories', 'MenuItems', 'Orders', 'OrderItems', 'Bills', 'Payments',
                     'LegacyOrders', 'LegacyProducts')
ORDER BY TABLE_NAME;
GO
