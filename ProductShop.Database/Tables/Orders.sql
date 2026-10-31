CREATE TABLE Orders (
    Id             BIGINT IDENTITY(1, 1) NOT NULL,
    ProductId      BIGINT NOT NULL,
    PublicId       CHAR(36) NOT NULL,
    Amount         DECIMAL(18, 2) NOT NULL,
    GatewayOrderId NVARCHAR(100) NOT NULL,
    Status         NVARCHAR(20) NOT NULL,
    CreatedAt      DATETIMEOFFSET NOT NULL,
    CONSTRAINT PK_Orders PRIMARY KEY (Id),
    CONSTRAINT UQ_Orders_GatewayOrderId UNIQUE (GatewayOrderId),
    CONSTRAINT UQ_Orders_PublicId UNIQUE (PublicId),
    CONSTRAINT CK_Orders_Status CHECK (Status IN ('Pending', 'Paid', 'Failed'))
);

CREATE NONCLUSTERED INDEX IX_Orders_ProductId ON Orders (ProductId);