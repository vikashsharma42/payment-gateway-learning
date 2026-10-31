CREATE TABLE Products (
    Id          BIGINT IDENTITY(1, 1) NOT NULL,
    Name        NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    Price       DECIMAL(18, 2) NOT NULL,
    ImageUrl    NVARCHAR(500) NOT NULL,
    CONSTRAINT PK_Product PRIMARY KEY (Id)
);
