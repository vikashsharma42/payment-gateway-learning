CREATE TABLE Customers (
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
    -- One customer per phone number (digits only)
    CONSTRAINT UQ_Customers_Phone UNIQUE (Phone),
    CONSTRAINT FK_Customers_Users FOREIGN KEY (ApplicationUserId) REFERENCES Users (Id)
);
