/*
    DineBook CREATE-ONLY schema script
    Generated from the supplied DineBook.Domain.zip entity classes.
    Creates 10 tables: Users, Customers, Restaurants, RestaurantTables,
    MenuCategories, MenuItems, Reservations, Orders, OrderItems, Payments.

    Mapping notes:
    - Address is stored as columns in Restaurants (owned value object).
    - ReservationPolicy and ReservationRules are not database tables.
    - Money is a value object, not a standalone entity/table.
    - Enum values are stored as text and protected by CHECK constraints.

    IMPORTANT:
    - Run this only against the DineBook database.
    - This script does NOT rename or modify Legacy_ProductShop_* tables.
    - It creates the DineBook tables only; it does not migrate old ProductShop data.
    - Back up the database before execution.
*/
USE [DineBook];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'DineBook'
    THROW 51000, 'Run this script only in the DineBook database.', 1;

/*
  Preflight is deliberately in the SAME batch as the DDL.
  This prevents later CREATE TABLE batches from running if a table already exists.
*/
IF EXISTS
(
    SELECT 1
    FROM sys.tables
    WHERE schema_id = SCHEMA_ID(N'dbo')
      AND name IN
      (
          N'Users', N'Customers', N'Restaurants', N'RestaurantTables',
          N'MenuCategories', N'MenuItems', N'Reservations', N'Orders',
          N'OrderItems', N'Payments'
      )
)
    THROW 51002, 'One or more DineBook tables already exist. Do not rerun this script on a partially created schema.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    /* Users */
    CREATE TABLE dbo.Users
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        Email NVARCHAR(254) NOT NULL,
        PasswordHash NVARCHAR(500) NOT NULL,
        FirstName NVARCHAR(100) NOT NULL,
        LastName NVARCHAR(100) NOT NULL,
        Role NVARCHAR(20) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
        CreatedDateTime DATETIMEOFFSET NOT NULL CONSTRAINT DF_Users_CreatedDateTime DEFAULT (SYSUTCDATETIME()),
        CreatedBy NVARCHAR(256) NULL,
        UpdatedDateTime DATETIMEOFFSET NOT NULL CONSTRAINT DF_Users_UpdatedDateTime DEFAULT (SYSUTCDATETIME()),
        UpdatedBy NVARCHAR(256) NULL,
        CONSTRAINT UQ_Users_Email UNIQUE (Email),
        CONSTRAINT CK_Users_Role CHECK (Role IN (N'Customer', N'Staff', N'Manager', N'Admin'))
    );

    /* Customers */
    CREATE TABLE dbo.Customers
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Phone NVARCHAR(15) NOT NULL,
        Email NVARCHAR(254) NOT NULL,
        ApplicationUserId BIGINT NULL,
        CreatedDateTime DATETIMEOFFSET NOT NULL CONSTRAINT DF_Customers_CreatedDateTime DEFAULT (SYSUTCDATETIME()),
        CreatedBy NVARCHAR(256) NULL,
        UpdatedDateTime DATETIMEOFFSET NOT NULL CONSTRAINT DF_Customers_UpdatedDateTime DEFAULT (SYSUTCDATETIME()),
        UpdatedBy NVARCHAR(256) NULL,
        CONSTRAINT UQ_Customers_Phone UNIQUE (Phone),
        CONSTRAINT CK_Customers_Phone CHECK (Phone NOT LIKE N'%[^0-9]%' AND LEN(Phone) BETWEEN 10 AND 15),
        CONSTRAINT FK_Customers_Users FOREIGN KEY (ApplicationUserId) REFERENCES dbo.Users(Id)
    );

    /* Restaurants: Address is an owned value object, stored as columns on Restaurants. */
    CREATE TABLE dbo.Restaurants
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Restaurants PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Address_Street NVARCHAR(200) NOT NULL,
        Address_City NVARCHAR(100) NOT NULL,
        Address_State NVARCHAR(100) NOT NULL,
        Address_Pincode NVARCHAR(6) NOT NULL,
        OpensAt TIME NOT NULL,
        ClosesAt TIME NOT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Restaurants_Status DEFAULT (N'Active'),
        Phone NVARCHAR(15) NOT NULL CONSTRAINT DF_Restaurants_Phone DEFAULT (N''),
        Description NVARCHAR(500) NOT NULL CONSTRAINT DF_Restaurants_Description DEFAULT (N''),
        Cuisine NVARCHAR(50) NOT NULL CONSTRAINT DF_Restaurants_Cuisine DEFAULT (N''),
        TaxType NVARCHAR(20) NOT NULL,
        TaxPercent DECIMAL(5,2) NOT NULL,
        SystemAccessFee DECIMAL(10,2) NOT NULL,
        CreatedDateTime DATETIMEOFFSET NOT NULL CONSTRAINT DF_Restaurants_CreatedDateTime DEFAULT (SYSUTCDATETIME()),
        CreatedBy NVARCHAR(256) NULL,
        UpdatedDateTime DATETIMEOFFSET NOT NULL CONSTRAINT DF_Restaurants_UpdatedDateTime DEFAULT (SYSUTCDATETIME()),
        UpdatedBy NVARCHAR(256) NULL,
        CONSTRAINT CK_Restaurants_Status CHECK (Status IN (N'Active', N'Inactive')),
        CONSTRAINT CK_Restaurants_Address_Pincode CHECK (Address_Pincode NOT LIKE N'%[^0-9]%' AND LEN(Address_Pincode) = 6)
    );

    /* Restaurant tables */
    CREATE TABLE dbo.RestaurantTables
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RestaurantTables PRIMARY KEY,
        RestaurantId BIGINT NOT NULL,
        Label NVARCHAR(20) NOT NULL,
        Capacity INT NOT NULL,
        Location NVARCHAR(50) NOT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_RestaurantTables_Status DEFAULT (N'Available'),
        IsDeleted BIT NOT NULL CONSTRAINT DF_RestaurantTables_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL,
        CreatedDateTime DATETIMEOFFSET NOT NULL CONSTRAINT DF_RestaurantTables_CreatedDateTime DEFAULT (SYSUTCDATETIME()),
        CreatedBy NVARCHAR(256) NULL,
        UpdatedDateTime DATETIMEOFFSET NOT NULL CONSTRAINT DF_RestaurantTables_UpdatedDateTime DEFAULT (SYSUTCDATETIME()),
        UpdatedBy NVARCHAR(256) NULL,
        CONSTRAINT FK_RestaurantTables_Restaurants FOREIGN KEY (RestaurantId) REFERENCES dbo.Restaurants(Id) ON DELETE CASCADE,
        CONSTRAINT CK_RestaurantTables_Capacity CHECK (Capacity BETWEEN 1 AND 20),
        CONSTRAINT CK_RestaurantTables_Status CHECK (Status IN (N'Available', N'Occupied', N'OutOfService'))
    );
    CREATE INDEX IX_RestaurantTables_RestaurantId ON dbo.RestaurantTables(RestaurantId);
    CREATE UNIQUE INDEX UX_RestaurantTables_RestaurantId_Label_Active
        ON dbo.RestaurantTables(RestaurantId, Label) WHERE IsDeleted = 0;

    /* Menu categories */
    CREATE TABLE dbo.MenuCategories
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MenuCategories PRIMARY KEY,
        RestaurantId BIGINT NOT NULL,
        Name NVARCHAR(50) NOT NULL,
        CreatedDateTime DATETIMEOFFSET NOT NULL CONSTRAINT DF_MenuCategories_CreatedDateTime DEFAULT (SYSUTCDATETIME()),
        CreatedBy NVARCHAR(256) NULL,
        UpdatedDateTime DATETIMEOFFSET NOT NULL CONSTRAINT DF_MenuCategories_UpdatedDateTime DEFAULT (SYSUTCDATETIME()),
        UpdatedBy NVARCHAR(256) NULL,
        CONSTRAINT FK_MenuCategories_Restaurants FOREIGN KEY (RestaurantId) REFERENCES dbo.Restaurants(Id) ON DELETE CASCADE,
        CONSTRAINT UQ_MenuCategories_RestaurantId_Name UNIQUE (RestaurantId, Name)
    );

    /* Menu items */
    CREATE TABLE dbo.MenuItems
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MenuItems PRIMARY KEY,
        RestaurantId BIGINT NOT NULL,
        MenuCategoryId BIGINT NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(300) NOT NULL,
        Category NVARCHAR(50) NOT NULL,
        Price DECIMAL(18,2) NOT NULL,
        IsAvailable BIT NOT NULL CONSTRAINT DF_MenuItems_IsAvailable DEFAULT (1),
        IsDeleted BIT NOT NULL CONSTRAINT DF_MenuItems_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL,
        CreatedDateTime DATETIMEOFFSET NOT NULL CONSTRAINT DF_MenuItems_CreatedDateTime DEFAULT (SYSUTCDATETIME()),
        CreatedBy NVARCHAR(256) NULL,
        UpdatedDateTime DATETIMEOFFSET NOT NULL CONSTRAINT DF_MenuItems_UpdatedDateTime DEFAULT (SYSUTCDATETIME()),
        UpdatedBy NVARCHAR(256) NULL,
        CONSTRAINT FK_MenuItems_Restaurants FOREIGN KEY (RestaurantId) REFERENCES dbo.Restaurants(Id),
        CONSTRAINT FK_MenuItems_MenuCategories FOREIGN KEY (MenuCategoryId) REFERENCES dbo.MenuCategories(Id),
        CONSTRAINT CK_MenuItems_Price CHECK (Price > 0 AND Price <= 100000)
    );
    CREATE INDEX IX_MenuItems_RestaurantId ON dbo.MenuItems(RestaurantId);
    CREATE INDEX IX_MenuItems_MenuCategoryId ON dbo.MenuItems(MenuCategoryId);

    /* Reservations */
    CREATE TABLE dbo.Reservations
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Reservations PRIMARY KEY,
        Code NVARCHAR(8) NOT NULL,
        RestaurantId BIGINT NOT NULL,
        CustomerId BIGINT NULL,
        TableId BIGINT NOT NULL,
        CustomerName NVARCHAR(100) NOT NULL,
        CustomerPhone NVARCHAR(15) NOT NULL,
        CustomerEmail NVARCHAR(254) NOT NULL,
        Guests INT NOT NULL,
        StartsAt DATETIME2 NOT NULL,
        DurationMinutes INT NOT NULL,
        SpecialRequests NVARCHAR(200) NOT NULL,
        Status NVARCHAR(20) NOT NULL,
        CreatedDateTime DATETIMEOFFSET NOT NULL CONSTRAINT DF_Reservations_CreatedDateTime DEFAULT (SYSUTCDATETIME()),
        CreatedBy NVARCHAR(256) NULL,
        UpdatedDateTime DATETIMEOFFSET NOT NULL CONSTRAINT DF_Reservations_UpdatedDateTime DEFAULT (SYSUTCDATETIME()),
        UpdatedBy NVARCHAR(256) NULL,
        CONSTRAINT UQ_Reservations_Code UNIQUE (Code),
        CONSTRAINT FK_Reservations_Restaurants FOREIGN KEY (RestaurantId) REFERENCES dbo.Restaurants(Id),
        CONSTRAINT FK_Reservations_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id),
        CONSTRAINT FK_Reservations_RestaurantTables FOREIGN KEY (TableId) REFERENCES dbo.RestaurantTables(Id),
        CONSTRAINT CK_Reservations_Phone CHECK (CustomerPhone NOT LIKE N'%[^0-9]%' AND LEN(CustomerPhone) BETWEEN 10 AND 15),
        CONSTRAINT CK_Reservations_Guests CHECK (Guests > 0),
        CONSTRAINT CK_Reservations_Duration CHECK (DurationMinutes >= 30),
        CONSTRAINT CK_Reservations_Status CHECK (Status IN (N'Confirmed', N'Seated', N'Completed', N'Cancelled', N'NoShow'))
    );
    CREATE INDEX IX_Reservations_RestaurantId ON dbo.Reservations(RestaurantId);
    CREATE INDEX IX_Reservations_CustomerId ON dbo.Reservations(CustomerId);
    CREATE INDEX IX_Reservations_TableId ON dbo.Reservations(TableId);
    CREATE INDEX IX_Reservations_StartsAt ON dbo.Reservations(StartsAt);

    /* Food orders */
    CREATE TABLE dbo.Orders
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
        ReservationId BIGINT NOT NULL,
        RestaurantId BIGINT NOT NULL,
        Status NVARCHAR(20) NOT NULL,
        CreatedDateTime DATETIMEOFFSET NOT NULL CONSTRAINT DF_Orders_CreatedDateTime DEFAULT (SYSUTCDATETIME()),
        CreatedBy NVARCHAR(256) NULL,
        UpdatedDateTime DATETIMEOFFSET NOT NULL CONSTRAINT DF_Orders_UpdatedDateTime DEFAULT (SYSUTCDATETIME()),
        UpdatedBy NVARCHAR(256) NULL,
        CONSTRAINT FK_Orders_Reservations FOREIGN KEY (ReservationId) REFERENCES dbo.Reservations(Id),
        CONSTRAINT FK_Orders_Restaurants FOREIGN KEY (RestaurantId) REFERENCES dbo.Restaurants(Id),
        CONSTRAINT CK_Orders_Status CHECK (Status IN (N'Placed', N'Served', N'Cancelled'))
    );
    CREATE INDEX IX_Orders_ReservationId ON dbo.Orders(ReservationId);
    CREATE INDEX IX_Orders_RestaurantId ON dbo.Orders(RestaurantId);

    /* Order items */
    CREATE TABLE dbo.OrderItems
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderItems PRIMARY KEY,
        OrderId BIGINT NOT NULL,
        MenuItemId BIGINT NOT NULL,
        ItemName NVARCHAR(100) NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL,
        Quantity INT NOT NULL,
        CreatedDateTime DATETIMEOFFSET NOT NULL CONSTRAINT DF_OrderItems_CreatedDateTime DEFAULT (SYSUTCDATETIME()),
        CreatedBy NVARCHAR(256) NULL,
        UpdatedDateTime DATETIMEOFFSET NOT NULL CONSTRAINT DF_OrderItems_UpdatedDateTime DEFAULT (SYSUTCDATETIME()),
        UpdatedBy NVARCHAR(256) NULL,
        CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
        CONSTRAINT FK_OrderItems_MenuItems FOREIGN KEY (MenuItemId) REFERENCES dbo.MenuItems(Id),
        CONSTRAINT CK_OrderItems_UnitPrice CHECK (UnitPrice > 0),
        CONSTRAINT CK_OrderItems_Quantity CHECK (Quantity BETWEEN 1 AND 20),
        CONSTRAINT UQ_OrderItems_OrderId_MenuItemId UNIQUE (OrderId, MenuItemId)
    );
    CREATE INDEX IX_OrderItems_MenuItemId ON dbo.OrderItems(MenuItemId);

    /* Payments: domain Payment references Orders directly (not Bills). */
    CREATE TABLE dbo.Payments
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Payments PRIMARY KEY,
        OrderId BIGINT NOT NULL,
        PublicId UNIQUEIDENTIFIER NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        GatewayOrderId NVARCHAR(100) NOT NULL,
        PaymentSessionId NVARCHAR(200) NOT NULL,
        IdempotencyKey NVARCHAR(100) NOT NULL,
        CustomerEmail NVARCHAR(254) NOT NULL,
        RefundId NVARCHAR(100) NULL,
        Status NVARCHAR(20) NOT NULL,
        CreatedDateTime DATETIMEOFFSET NOT NULL CONSTRAINT DF_Payments_CreatedDateTime DEFAULT (SYSUTCDATETIME()),
        CreatedBy NVARCHAR(256) NULL,
        UpdatedDateTime DATETIMEOFFSET NOT NULL CONSTRAINT DF_Payments_UpdatedDateTime DEFAULT (SYSUTCDATETIME()),
        UpdatedBy NVARCHAR(256) NULL,
        CONSTRAINT FK_Payments_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders(Id),
        CONSTRAINT UQ_Payments_PublicId UNIQUE (PublicId),
        CONSTRAINT UQ_Payments_GatewayOrderId UNIQUE (GatewayOrderId),
        CONSTRAINT UQ_Payments_IdempotencyKey UNIQUE (IdempotencyKey),
        CONSTRAINT CK_Payments_Amount CHECK (Amount > 0),
        CONSTRAINT CK_Payments_Status CHECK (Status IN (N'Pending', N'Paid', N'Failed', N'Refunded'))
    );
    CREATE INDEX IX_Payments_OrderId ON dbo.Payments(OrderId);


    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

/* Verify created tables. */
SELECT TABLE_SCHEMA, TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
  AND TABLE_NAME IN
  (
      N'Users', N'Customers', N'Restaurants', N'RestaurantTables',
      N'MenuCategories', N'MenuItems', N'Reservations', N'Orders',
      N'OrderItems', N'Payments'
  )
ORDER BY TABLE_NAME;
GO
