CREATE TABLE Users (
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
