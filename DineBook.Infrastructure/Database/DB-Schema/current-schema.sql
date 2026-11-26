/*
  DineBook - schema that matches the current EF Core model.

  The project does not use EF migrations. Run this script once on an empty database
  (or on a database created by an older script, after taking a backup: it only creates
  missing tables, it never changes existing ones).

  Every statement is guarded, so running it twice is safe.
  Money value objects are stored as <Property>_Amount and <Property>_Currency.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.Restaurant', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Restaurant (
        Id                BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Restaurant PRIMARY KEY,
        Name              NVARCHAR(100)  NOT NULL,
        Slug              NVARCHAR(60)   NOT NULL,
        OpensAt           TIME(7)        NOT NULL,
        ClosesAt          TIME(7)        NOT NULL,
        Status            NVARCHAR(20)   NOT NULL,
        Phone             NVARCHAR(15)   NULL,
        Description       NVARCHAR(500)  NULL,
        TaxPercent        DECIMAL(5,2)   NOT NULL,
        SystemAccessFee   DECIMAL(18,2)  NOT NULL,
        TaxType           NVARCHAR(20)   NOT NULL,
        CreatedDateTime   DATETIMEOFFSET NOT NULL,
        CreatedBy         NVARCHAR(256)  NULL,
        UpdatedDateTime   DATETIMEOFFSET NOT NULL,
        UpdatedBy         NVARCHAR(256)  NULL
    );
    CREATE UNIQUE INDEX IX_Restaurant_Slug ON dbo.Restaurant (Slug);
END
GO

IF OBJECT_ID(N'dbo.ApplicationUser', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApplicationUser (
        Id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ApplicationUser PRIMARY KEY,
        Email           NVARCHAR(254)  NOT NULL,
        PasswordHash    NVARCHAR(500)  NOT NULL,
        FirstName       NVARCHAR(100)  NOT NULL,
        LastName        NVARCHAR(100)  NOT NULL,
        Role            NVARCHAR(20)   NOT NULL,
        Phone           NVARCHAR(15)   NULL,
        RestaurantId    BIGINT         NULL CONSTRAINT FK_ApplicationUser_Restaurant REFERENCES dbo.Restaurant (Id),
        IsActive        BIT            NOT NULL,
        CreatedDateTime DATETIMEOFFSET NOT NULL,
        CreatedBy       NVARCHAR(256)  NULL,
        UpdatedDateTime DATETIMEOFFSET NOT NULL,
        UpdatedBy       NVARCHAR(256)  NULL
    );
    CREATE UNIQUE INDEX IX_ApplicationUser_Email ON dbo.ApplicationUser (Email);
    CREATE UNIQUE INDEX IX_ApplicationUser_Phone ON dbo.ApplicationUser (Phone) WHERE Phone IS NOT NULL;
END
GO

IF OBJECT_ID(N'dbo.Location', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Location (
        Id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Location PRIMARY KEY,
        RestaurantId    BIGINT         NOT NULL CONSTRAINT FK_Location_Restaurant REFERENCES dbo.Restaurant (Id) ON DELETE CASCADE,
        Name            NVARCHAR(100)  NOT NULL,
        Phone           NVARCHAR(15)   NULL,
        IsActive        BIT            NOT NULL,
        Street          NVARCHAR(200)  NOT NULL,
        City            NVARCHAR(100)  NOT NULL,
        State           NVARCHAR(100)  NOT NULL,
        Country         NVARCHAR(100)  NOT NULL,
        ZipCode         NVARCHAR(20)   NOT NULL,
        CreatedDateTime DATETIMEOFFSET NOT NULL,
        CreatedBy       NVARCHAR(256)  NULL,
        UpdatedDateTime DATETIMEOFFSET NOT NULL,
        UpdatedBy       NVARCHAR(256)  NULL
    );
END
GO

IF OBJECT_ID(N'dbo.[Table]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Table] (
        Id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Table PRIMARY KEY,
        RestaurantId    BIGINT         NOT NULL CONSTRAINT FK_Table_Restaurant REFERENCES dbo.Restaurant (Id) ON DELETE CASCADE,
        LocationId      BIGINT         NOT NULL CONSTRAINT FK_Table_Location REFERENCES dbo.Location (Id),
        Label           NVARCHAR(20)   NOT NULL,
        Capacity        INT            NOT NULL,
        Location        NVARCHAR(100)  NOT NULL,
        Status          NVARCHAR(20)   NOT NULL,
        IsDeleted       BIT            NOT NULL,
        DeletedAt       DATETIMEOFFSET NULL,
        CreatedDateTime DATETIMEOFFSET NOT NULL,
        CreatedBy       NVARCHAR(256)  NULL,
        UpdatedDateTime DATETIMEOFFSET NOT NULL,
        UpdatedBy       NVARCHAR(256)  NULL
    );
    CREATE UNIQUE INDEX IX_Table_Location_Label ON dbo.[Table] (LocationId, Label) WHERE IsDeleted = 0;
END
GO

IF OBJECT_ID(N'dbo.MenuCategory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MenuCategory (
        Id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MenuCategory PRIMARY KEY,
        RestaurantId    BIGINT         NOT NULL CONSTRAINT FK_MenuCategory_Restaurant REFERENCES dbo.Restaurant (Id) ON DELETE CASCADE,
        Name            NVARCHAR(50)   NOT NULL,
        CreatedDateTime DATETIMEOFFSET NOT NULL,
        CreatedBy       NVARCHAR(256)  NULL,
        UpdatedDateTime DATETIMEOFFSET NOT NULL,
        UpdatedBy       NVARCHAR(256)  NULL
    );
    CREATE UNIQUE INDEX IX_MenuCategory_Restaurant_Name ON dbo.MenuCategory (RestaurantId, Name);
END
GO

IF OBJECT_ID(N'dbo.MenuItem', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MenuItem (
        Id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MenuItem PRIMARY KEY,
        RestaurantId    BIGINT         NOT NULL CONSTRAINT FK_MenuItem_Restaurant REFERENCES dbo.Restaurant (Id) ON DELETE CASCADE,
        MenuCategoryId  BIGINT         NOT NULL CONSTRAINT FK_MenuItem_MenuCategory REFERENCES dbo.MenuCategory (Id),
        Name            NVARCHAR(100)  NOT NULL,
        Description     NVARCHAR(300)  NULL,
        Category        NVARCHAR(50)   NULL,
        Price_Amount    DECIMAL(18,2)  NOT NULL,
        Price_Currency  NVARCHAR(3)    NOT NULL,
        ImageUrl        NVARCHAR(50)   NULL,
        IsAvailable     BIT            NOT NULL,
        DeletedAt       DATETIMEOFFSET NULL,
        CreatedDateTime DATETIMEOFFSET NOT NULL,
        CreatedBy       NVARCHAR(256)  NULL,
        UpdatedDateTime DATETIMEOFFSET NOT NULL,
        UpdatedBy       NVARCHAR(256)  NULL
    );
    CREATE INDEX IX_MenuItem_Restaurant_Available ON dbo.MenuItem (RestaurantId, IsAvailable);
END
GO

IF OBJECT_ID(N'dbo.Reservation', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Reservation (
        Id                BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Reservation PRIMARY KEY,
        Code              NVARCHAR(8)    NOT NULL,
        RestaurantId      BIGINT         NOT NULL CONSTRAINT FK_Reservation_Restaurant REFERENCES dbo.Restaurant (Id),
        ApplicationUserId BIGINT         NOT NULL CONSTRAINT FK_Reservation_ApplicationUser REFERENCES dbo.ApplicationUser (Id),
        TableId           BIGINT         NOT NULL CONSTRAINT FK_Reservation_Table REFERENCES dbo.[Table] (Id),
        Guests            INT            NOT NULL,
        StartsAt          DATETIME2(7)   NOT NULL,
        DurationMinutes   INT            NOT NULL,
        SpecialRequests   NVARCHAR(200)  NULL,
        Status            NVARCHAR(20)   NOT NULL,
        CreatedDateTime   DATETIMEOFFSET NOT NULL,
        CreatedBy         NVARCHAR(256)  NULL,
        UpdatedDateTime   DATETIMEOFFSET NOT NULL,
        UpdatedBy         NVARCHAR(256)  NULL
    );
    CREATE UNIQUE INDEX IX_Reservation_Code ON dbo.Reservation (Code);
    CREATE INDEX IX_Reservation_Restaurant_StartsAt ON dbo.Reservation (RestaurantId, StartsAt);
END
GO

IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders (
        Id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
        ReservationId   BIGINT         NOT NULL CONSTRAINT FK_Orders_Reservation REFERENCES dbo.Reservation (Id) ON DELETE CASCADE,
        RestaurantId    BIGINT         NOT NULL CONSTRAINT FK_Orders_Restaurant REFERENCES dbo.Restaurant (Id),
        OrderNo         NVARCHAR(20)   NOT NULL,
        Status          NVARCHAR(20)   NOT NULL,
        CreatedDateTime DATETIMEOFFSET NOT NULL,
        CreatedBy       NVARCHAR(256)  NULL,
        UpdatedDateTime DATETIMEOFFSET NOT NULL,
        UpdatedBy       NVARCHAR(256)  NULL
    );
    CREATE UNIQUE INDEX IX_Orders_OrderNo ON dbo.Orders (OrderNo);
END
GO

-- Existing databases created before OrderNo existed: add it, fill it for old rows, then make it required.
IF COL_LENGTH(N'dbo.Orders', N'OrderNo') IS NULL
BEGIN
    ALTER TABLE dbo.Orders ADD OrderNo NVARCHAR(20) NULL;
END
GO

IF EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderNo IS NULL)
BEGIN
    UPDATE dbo.Orders
    SET OrderNo = 'ORD-' + CONVERT(CHAR(8), CreatedDateTime, 112) + '-'
                  + LEFT(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''), 6)
    WHERE OrderNo IS NULL;
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Orders') AND name = N'OrderNo' AND is_nullable = 1)
BEGIN
    ALTER TABLE dbo.Orders ALTER COLUMN OrderNo NVARCHAR(20) NOT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Orders') AND name = N'IX_Orders_OrderNo')
BEGIN
    CREATE UNIQUE INDEX IX_Orders_OrderNo ON dbo.Orders (OrderNo);
END
GO

IF OBJECT_ID(N'dbo.OrderItem', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderItem (
        Id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderItem PRIMARY KEY,
        OrderId         BIGINT         NOT NULL CONSTRAINT FK_OrderItem_Orders REFERENCES dbo.Orders (Id) ON DELETE CASCADE,
        MenuItemId      BIGINT         NOT NULL CONSTRAINT FK_OrderItem_MenuItem REFERENCES dbo.MenuItem (Id),
        ItemName        NVARCHAR(100)  NOT NULL,
        UnitPrice_Amount   DECIMAL(18,2) NOT NULL,
        UnitPrice_Currency NVARCHAR(3)   NOT NULL,
        Quantity        INT            NOT NULL,
        ImageUrl        NVARCHAR(50)   NULL,
        CreatedDateTime DATETIMEOFFSET NOT NULL,
        CreatedBy       NVARCHAR(256)  NULL,
        UpdatedDateTime DATETIMEOFFSET NOT NULL,
        UpdatedBy       NVARCHAR(256)  NULL
    );
END
GO

IF OBJECT_ID(N'dbo.Payment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Payment (
        Id                BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Payment PRIMARY KEY,
        OrderId           BIGINT         NOT NULL CONSTRAINT FK_Payment_Orders REFERENCES dbo.Orders (Id),
        PublicId          UNIQUEIDENTIFIER NOT NULL,
        Amount_Amount     DECIMAL(18,2)  NOT NULL,
        Amount_Currency   NVARCHAR(3)    NOT NULL,
        GatewayOrderId    NVARCHAR(100)  NOT NULL,
        PaymentSessionId  NVARCHAR(200)  NOT NULL,
        IdempotencyKey    NVARCHAR(64)   NOT NULL,
        CustomerEmail     NVARCHAR(254)  NOT NULL,
        RefundId          NVARCHAR(100)  NULL,
        Status            NVARCHAR(20)   NOT NULL,
        CreatedDateTime   DATETIMEOFFSET NOT NULL,
        CreatedBy         NVARCHAR(256)  NULL,
        UpdatedDateTime   DATETIMEOFFSET NOT NULL,
        UpdatedBy         NVARCHAR(256)  NULL
    );
    CREATE UNIQUE INDEX IX_Payment_GatewayOrderId ON dbo.Payment (GatewayOrderId);
    CREATE UNIQUE INDEX IX_Payment_IdempotencyKey ON dbo.Payment (IdempotencyKey) WHERE IdempotencyKey IS NOT NULL;
END
GO
