CREATE TABLE MenuCategories (
    Id           BIGINT IDENTITY(1, 1) NOT NULL,
    RestaurantId BIGINT NOT NULL,
    Name         NVARCHAR(50) NOT NULL,
    CreatedDateTime    DATETIMEOFFSET NOT NULL CONSTRAINT DF_MenuCategories_CreatedDateTime DEFAULT SYSUTCDATETIME(),
    UpdatedDateTime    DATETIMEOFFSET NOT NULL CONSTRAINT DF_MenuCategories_UpdatedDateTime DEFAULT SYSUTCDATETIME(),
    CreatedBy         NVARCHAR(256) NULL,
    UpdatedBy         NVARCHAR(256) NULL,
    CONSTRAINT PK_MenuCategories PRIMARY KEY (Id),
    -- Deleting a restaurant removes its categories
    CONSTRAINT FK_MenuCategories_Restaurants FOREIGN KEY (RestaurantId) REFERENCES Restaurants (Id) ON DELETE CASCADE,
    CONSTRAINT UQ_MenuCategories_RestaurantId_Name UNIQUE (RestaurantId, Name)
);
