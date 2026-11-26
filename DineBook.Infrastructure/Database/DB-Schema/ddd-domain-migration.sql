/*
    DineBook CREATE-ONLY schema script, matching the current Domain (DineBook.Domain)
    and the EF Core mapping (DineBook.Infrastructure/Database/Configurations).

    Tables (10):
      ApplicationUser, Restaurant, Location, Table, MenuCategory, MenuItem,
      Reservation, Orders, OrderItem, Payment

    Mapping notes:
    - Address (Location) and Money (MenuItem.Price, OrderItem.UnitPrice, Payment.Amount)
      are owned value objects stored as columns in the owning table.
      Money columns are <Property>_Amount and <Property>_Currency.
    - Enums are stored as text and protected by CHECK constraints.
    - Table.Location is the location name (free text copied from Location).
    - Orders.OrderNo is unique (for example ORD-20261126-K7Q2MX).
    - Tables are soft-deleted with IsDeleted; a removed label can be reused in the same location.
    - Customer details for a booking come from ApplicationUser (Reservation.ApplicationUserId).

    IMPORTANT:
    - Run this only against the DineBook database, on a database with none of these tables.
    - It creates tables only. It does not migrate old data.
    - Back up the database before running it.
*/
USE [DineBook];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'DineBook'
    THROW 51000, 'Run this script only in the DineBook database.', 1;

-- Preflight in the same batch as the DDL, so a partly created schema is never extended
IF EXISTS
(
    SELECT 1 FROM sys.tables
    WHERE schema_id = SCHEMA_ID(N'dbo')
      AND name IN (N'ApplicationUser', N'Restaurant', N'Location', N'Table', N'MenuCategory',
                   N'MenuItem', N'Reservation', N'Orders', N'OrderItem', N'Payment')
)
    THROW 51002, 'One or more DineBook tables already exist. Do not rerun this script on a partly created schema.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    /* Restaurant (Organization). Slug is the unique web address, for example spice-garden. */
    CREATE TABLE dbo.Restaurant
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Restaurant PRIMARY KEY,
        Name            NVARCHAR(100)  NOT NULL,
        Slug            NVARCHAR(60)   NOT NULL,
        OpensAt         TIME(7)        NOT NULL,
        ClosesAt        TIME(7)        NOT NULL,
        Status          NVARCHAR(20)   NOT NULL,
        Phone           NVARCHAR(15)   NULL,
        Description     NVARCHAR(500)  NULL,
        TaxPercent      DECIMAL(5,2)   NOT NULL,
        SystemAccessFee DECIMAL(18,2)  NOT NULL,
        TaxType         NVARCHAR(20)   NOT NULL,
        CreatedDateTime DATETIMEOFFSET NOT NULL,
        CreatedBy       NVARCHAR(256)  NULL,
        UpdatedDateTime DATETIMEOFFSET NOT NULL,
        UpdatedBy       NVARCHAR(256)  NULL,
        CONSTRAINT CK_Restaurant_Status CHECK (Status IN (N'Active', N'Inactive')),
        CONSTRAINT CK_Restaurant_TaxPercent CHECK (TaxPercent BETWEEN 0 AND 28),
        CONSTRAINT CK_Restaurant_SystemAccessFee CHECK (SystemAccessFee BETWEEN 0 AND 500)
    );
    CREATE UNIQUE INDEX IX_Restaurant_Slug ON dbo.Restaurant (Slug);

    /* ApplicationUser (Identity). Customers and staff belong to a restaurant; admins do not. */
    CREATE TABLE dbo.ApplicationUser
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ApplicationUser PRIMARY KEY,
        Email           NVARCHAR(254) NOT NULL,
        PasswordHash    NVARCHAR(500) NOT NULL,
        FirstName       NVARCHAR(100) NOT NULL,
        LastName        NVARCHAR(100) NOT NULL,
        Role            NVARCHAR(20)  NOT NULL,
        Phone           NVARCHAR(15)  NULL,
        RestaurantId    BIGINT        NULL,
        IsActive        BIT           NOT NULL,
        CreatedDateTime DATETIMEOFFSET NOT NULL,
        CreatedBy       NVARCHAR(256) NULL,
        UpdatedDateTime DATETIMEOFFSET NOT NULL,
        UpdatedBy       NVARCHAR(256) NULL,
        CONSTRAINT FK_ApplicationUser_Restaurant FOREIGN KEY (RestaurantId) REFERENCES dbo.Restaurant (Id),
        CONSTRAINT CK_ApplicationUser_Role CHECK (Role IN (N'Customer', N'Staff', N'Manager', N'Admin')),
        CONSTRAINT CK_ApplicationUser_Restaurant CHECK
        (
            (Role = N'Admin' AND RestaurantId IS NULL)
            OR (Role <> N'Admin' AND RestaurantId IS NOT NULL)
        )
    );
    CREATE UNIQUE INDEX IX_ApplicationUser_Email ON dbo.ApplicationUser (Email);
    CREATE UNIQUE INDEX IX_ApplicationUser_Phone ON dbo.ApplicationUser (Phone) WHERE Phone IS NOT NULL;

    /* Location (branch of a restaurant). Address is owned: columns Street, City, State, Country, ZipCode. */
    CREATE TABLE dbo.Location
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Location PRIMARY KEY,
        RestaurantId    BIGINT         NOT NULL,
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
        UpdatedBy       NVARCHAR(256)  NULL,
        CONSTRAINT FK_Location_Restaurant FOREIGN KEY (RestaurantId) REFERENCES dbo.Restaurant (Id)
    );

    /* Table (seats at one location). Labels are unique per location among active (not removed) tables. */
    CREATE TABLE dbo.[Table]
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Table PRIMARY KEY,
        RestaurantId    BIGINT         NOT NULL,
        LocationId      BIGINT         NOT NULL,
        Label           NVARCHAR(20)   NOT NULL,
        Capacity        INT            NOT NULL,
        Location        NVARCHAR(100)  NOT NULL,
        Status          NVARCHAR(20)   NOT NULL,
        IsDeleted       BIT            NOT NULL,
        DeletedAt       DATETIMEOFFSET NULL,
        CreatedDateTime DATETIMEOFFSET NOT NULL,
        CreatedBy       NVARCHAR(256)  NULL,
        UpdatedDateTime DATETIMEOFFSET NOT NULL,
        UpdatedBy       NVARCHAR(256)  NULL,
        CONSTRAINT FK_Table_Restaurant FOREIGN KEY (RestaurantId) REFERENCES dbo.Restaurant (Id),
        CONSTRAINT FK_Table_Location FOREIGN KEY (LocationId) REFERENCES dbo.Location (Id),
        CONSTRAINT CK_Table_Capacity CHECK (Capacity BETWEEN 1 AND 20),
        CONSTRAINT CK_Table_Status CHECK (Status IN (N'Available', N'Occupied', N'OutOfService'))
    );
    CREATE UNIQUE INDEX IX_Table_Location_Label ON dbo.[Table] (LocationId, Label) WHERE IsDeleted = 0;

    /* MenuCategory (section of a menu, for example Starters). */
    CREATE TABLE dbo.MenuCategory
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MenuCategory PRIMARY KEY,
        RestaurantId    BIGINT         NOT NULL,
        Name            NVARCHAR(50)   NOT NULL,
        CreatedDateTime DATETIMEOFFSET NOT NULL,
        CreatedBy       NVARCHAR(256)  NULL,
        UpdatedDateTime DATETIMEOFFSET NOT NULL,
        UpdatedBy       NVARCHAR(256)  NULL,
        CONSTRAINT FK_MenuCategory_Restaurant FOREIGN KEY (RestaurantId) REFERENCES dbo.Restaurant (Id)
    );
    CREATE UNIQUE INDEX IX_MenuCategory_Restaurant_Name ON dbo.MenuCategory (RestaurantId, Name);

    /* MenuItem (dish). Price is Money: Price_Amount and Price_Currency. */
    CREATE TABLE dbo.MenuItem
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MenuItem PRIMARY KEY,
        RestaurantId    BIGINT         NOT NULL,
        MenuCategoryId  BIGINT         NOT NULL,
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
        UpdatedBy       NVARCHAR(256)  NULL,
        CONSTRAINT FK_MenuItem_Restaurant FOREIGN KEY (RestaurantId) REFERENCES dbo.Restaurant (Id),
        CONSTRAINT FK_MenuItem_MenuCategory FOREIGN KEY (MenuCategoryId) REFERENCES dbo.MenuCategory (Id),
        CONSTRAINT CK_MenuItem_Price CHECK (Price_Amount > 0 AND Price_Amount <= 100000)
    );
    CREATE INDEX IX_MenuItem_Restaurant_Available ON dbo.MenuItem (RestaurantId, IsAvailable);

    /* Reservation (booking). Guest name, phone and email are read from ApplicationUser. */
    CREATE TABLE dbo.Reservation
    (
        Id                BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Reservation PRIMARY KEY,
        Code              NVARCHAR(8)    NOT NULL,
        RestaurantId      BIGINT         NOT NULL,
        ApplicationUserId BIGINT         NOT NULL,
        TableId           BIGINT         NOT NULL,
        Guests            INT            NOT NULL,
        StartsAt          DATETIME2(7)   NOT NULL,
        DurationMinutes   INT            NOT NULL,
        SpecialRequests   NVARCHAR(200)  NULL,
        Status            NVARCHAR(20)   NOT NULL,
        CreatedDateTime   DATETIMEOFFSET NOT NULL,
        CreatedBy         NVARCHAR(256)  NULL,
        UpdatedDateTime   DATETIMEOFFSET NOT NULL,
        UpdatedBy         NVARCHAR(256)  NULL,
        CONSTRAINT FK_Reservation_Restaurant FOREIGN KEY (RestaurantId) REFERENCES dbo.Restaurant (Id),
        CONSTRAINT FK_Reservation_ApplicationUser FOREIGN KEY (ApplicationUserId) REFERENCES dbo.ApplicationUser (Id),
        CONSTRAINT FK_Reservation_Table FOREIGN KEY (TableId) REFERENCES dbo.[Table] (Id),
        CONSTRAINT CK_Reservation_Guests CHECK (Guests >= 1),
        CONSTRAINT CK_Reservation_Duration CHECK (DurationMinutes >= 30),
        CONSTRAINT CK_Reservation_Status CHECK (Status IN (N'Confirmed', N'Seated', N'Completed', N'Cancelled', N'NoShow'))
    );
    CREATE UNIQUE INDEX IX_Reservation_Code ON dbo.Reservation (Code);
    CREATE INDEX IX_Reservation_Restaurant_StartsAt ON dbo.Reservation (RestaurantId, StartsAt);

    /* Orders (food order for one booking). OrderNo is created by OrderNumberHelper. */
    CREATE TABLE dbo.Orders
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
        ReservationId   BIGINT         NOT NULL,
        RestaurantId    BIGINT         NOT NULL,
        OrderNo         NVARCHAR(20)   NOT NULL,
        Status          NVARCHAR(20)   NOT NULL,
        CreatedDateTime DATETIMEOFFSET NOT NULL,
        CreatedBy       NVARCHAR(256)  NULL,
        UpdatedDateTime DATETIMEOFFSET NOT NULL,
        UpdatedBy       NVARCHAR(256)  NULL,
        CONSTRAINT FK_Orders_Reservation FOREIGN KEY (ReservationId) REFERENCES dbo.Reservation (Id),
        CONSTRAINT FK_Orders_Restaurant FOREIGN KEY (RestaurantId) REFERENCES dbo.Restaurant (Id),
        CONSTRAINT CK_Orders_Status CHECK (Status IN (N'Placed', N'Served', N'Cancelled'))
    );
    CREATE UNIQUE INDEX IX_Orders_OrderNo ON dbo.Orders (OrderNo);

    /* OrderItem (one dish in an order). Name, price and photo are copied at ordering time. */
    CREATE TABLE dbo.OrderItem
    (
        Id                 BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderItem PRIMARY KEY,
        OrderId            BIGINT         NOT NULL,
        MenuItemId         BIGINT         NOT NULL,
        ItemName           NVARCHAR(100)  NOT NULL,
        UnitPrice_Amount   DECIMAL(18,2)  NOT NULL,
        UnitPrice_Currency NVARCHAR(3)    NOT NULL,
        Quantity           INT            NOT NULL,
        ImageUrl           NVARCHAR(50)   NULL,
        CreatedDateTime    DATETIMEOFFSET NOT NULL,
        CreatedBy          NVARCHAR(256)  NULL,
        UpdatedDateTime    DATETIMEOFFSET NOT NULL,
        UpdatedBy          NVARCHAR(256)  NULL,
        CONSTRAINT FK_OrderItem_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders (Id),
        CONSTRAINT FK_OrderItem_MenuItem FOREIGN KEY (MenuItemId) REFERENCES dbo.MenuItem (Id),
        CONSTRAINT CK_OrderItem_UnitPrice CHECK (UnitPrice_Amount > 0),
        CONSTRAINT CK_OrderItem_Quantity CHECK (Quantity BETWEEN 1 AND 20)
    );

    /* Payment (one Cashfree attempt for one food order). Amount is Money: Amount_Amount and Amount_Currency. */
    CREATE TABLE dbo.Payment
    (
        Id                BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Payment PRIMARY KEY,
        OrderId           BIGINT           NOT NULL,
        PublicId          UNIQUEIDENTIFIER NOT NULL,
        Amount_Amount     DECIMAL(18,2)    NOT NULL,
        Amount_Currency   NVARCHAR(3)      NOT NULL,
        GatewayOrderId    NVARCHAR(100)    NOT NULL,
        PaymentSessionId  NVARCHAR(200)    NOT NULL,
        IdempotencyKey    NVARCHAR(64)     NOT NULL,
        CustomerEmail     NVARCHAR(254)    NOT NULL,
        RefundId          NVARCHAR(100)    NULL,
        Status            NVARCHAR(20)     NOT NULL,
        CreatedDateTime   DATETIMEOFFSET   NOT NULL,
        CreatedBy         NVARCHAR(256)    NULL,
        UpdatedDateTime   DATETIMEOFFSET   NOT NULL,
        UpdatedBy         NVARCHAR(256)    NULL,
        CONSTRAINT FK_Payment_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders (Id),
        CONSTRAINT CK_Payment_Amount CHECK (Amount_Amount > 0),
        CONSTRAINT CK_Payment_Status CHECK (Status IN (N'Pending', N'Paid', N'Failed', N'Refunded'))
    );
    CREATE UNIQUE INDEX IX_Payment_GatewayOrderId ON dbo.Payment (GatewayOrderId);
    CREATE UNIQUE INDEX IX_Payment_IdempotencyKey ON dbo.Payment (IdempotencyKey);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

/* Verify the created tables. */
SELECT TABLE_SCHEMA, TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
  AND TABLE_NAME IN (N'ApplicationUser', N'Restaurant', N'Location', N'Table', N'MenuCategory',
                     N'MenuItem', N'Reservation', N'Orders', N'OrderItem', N'Payment')
ORDER BY TABLE_NAME;
GO