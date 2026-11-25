-- Restaurant locations. A restaurant can have several branches, each with its own address and tables.
-- Moves the address from Restaurant to RestaurantLocation, puts each table at a location,
-- and links staff accounts to their restaurant.
-- Run once on each database. Safe to run again: each step checks whether it is already done.
-- Stops, and changes nothing more, when a table has no location.

-- 1. The locations table
IF OBJECT_ID('RestaurantLocation', 'U') IS NULL
CREATE TABLE RestaurantLocation (
    Id                 BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RestaurantLocation PRIMARY KEY,
    RestaurantId       BIGINT NOT NULL CONSTRAINT FK_RestaurantLocation_Restaurant REFERENCES Restaurant (Id) ON DELETE CASCADE,
    Name               NVARCHAR(100) NOT NULL,
    Phone              NVARCHAR(15) NOT NULL CONSTRAINT DF_RestaurantLocation_Phone DEFAULT (N''),
    Street             NVARCHAR(200) NOT NULL,
    City               NVARCHAR(100) NOT NULL,
    State              NVARCHAR(100) NOT NULL,
    Country            NVARCHAR(100) NOT NULL,
    ZipCode            NVARCHAR(20) NOT NULL,
    IsActive           BIT NOT NULL CONSTRAINT DF_RestaurantLocation_IsActive DEFAULT (1),
    CreatedDateTime    DATETIMEOFFSET NOT NULL CONSTRAINT DF_RestaurantLocation_CreatedDateTime DEFAULT (SYSUTCDATETIME()),
    CreatedBy          NVARCHAR(256) NOT NULL CONSTRAINT DF_RestaurantLocation_CreatedBy DEFAULT (N'system'),
    UpdatedDateTime    DATETIMEOFFSET NOT NULL CONSTRAINT DF_RestaurantLocation_UpdatedDateTime DEFAULT (SYSUTCDATETIME()),
    UpdatedBy          NVARCHAR(256) NOT NULL CONSTRAINT DF_RestaurantLocation_UpdatedBy DEFAULT (N'system')
);
GO

-- 2. Each restaurant's current address becomes its first location, named Main.
--    Country is set to India, because every address so far was an Indian pincode. Change it where needed.
IF COL_LENGTH('Restaurant', 'Street') IS NOT NULL
    INSERT INTO RestaurantLocation (RestaurantId, Name, Street, City, State, Country, ZipCode)
    SELECT r.Id, N'Main', r.Street, r.City, r.State, N'India', r.Pincode
    FROM Restaurant r
    WHERE NOT EXISTS (SELECT 1 FROM RestaurantLocation l WHERE l.RestaurantId = r.Id);
GO

-- 3. Tables move to their restaurant's first location
IF COL_LENGTH('RestaurantTable', 'RestaurantLocationId') IS NULL
    ALTER TABLE RestaurantTable ADD RestaurantLocationId BIGINT NULL;
GO

UPDATE t
SET t.RestaurantLocationId = (SELECT MIN(l.Id) FROM RestaurantLocation l WHERE l.RestaurantId = t.RestaurantId)
FROM RestaurantTable t
WHERE t.RestaurantLocationId IS NULL;
GO

IF EXISTS (SELECT 1 FROM RestaurantTable WHERE RestaurantLocationId IS NULL)
    THROW 50000, 'Some tables have no location. Check their restaurants, then run this script again.', 1;
GO

ALTER TABLE RestaurantTable ALTER COLUMN RestaurantLocationId BIGINT NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_RestaurantTable_RestaurantLocation')
    ALTER TABLE RestaurantTable ADD CONSTRAINT FK_RestaurantTable_RestaurantLocation
        FOREIGN KEY (RestaurantLocationId) REFERENCES RestaurantLocation (Id);
GO

-- Table labels are unique per location now, not per restaurant (active tables only)
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RestaurantTable_RestaurantId_Label' AND object_id = OBJECT_ID('RestaurantTable'))
    DROP INDEX IX_RestaurantTable_RestaurantId_Label ON RestaurantTable;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_RestaurantTable_Location_Label' AND object_id = OBJECT_ID('RestaurantTable'))
    CREATE UNIQUE INDEX UX_RestaurantTable_Location_Label ON RestaurantTable (RestaurantLocationId, Label)
        WHERE IsDeleted = 0;
GO

-- 4. Staff and managers belong to one restaurant. Admins and customers keep NULL.
IF COL_LENGTH('ApplicationUser', 'RestaurantId') IS NULL
    ALTER TABLE ApplicationUser ADD RestaurantId BIGINT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ApplicationUser_Restaurant')
    ALTER TABLE ApplicationUser ADD CONSTRAINT FK_ApplicationUser_Restaurant
        FOREIGN KEY (RestaurantId) REFERENCES Restaurant (Id);
GO

-- Staff with no restaurant yet: pick one for each in /Admin/Users (or with an UPDATE)
SELECT Id, Email, Role FROM ApplicationUser WHERE Role IN ('Staff', 'Manager') AND RestaurantId IS NULL;
GO

-- 5. The address now lives on the location. Remove it from Restaurant once every restaurant has a location.
IF COL_LENGTH('Restaurant', 'Street') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM Restaurant r WHERE NOT EXISTS (SELECT 1 FROM RestaurantLocation l WHERE l.RestaurantId = r.Id))
        THROW 50000, 'Some restaurants have no location. Check them, then run this script again.', 1;

    ALTER TABLE Restaurant DROP COLUMN Street;
    ALTER TABLE Restaurant DROP COLUMN City;
    ALTER TABLE Restaurant DROP COLUMN State;
    ALTER TABLE Restaurant DROP COLUMN Pincode;
END
GO
