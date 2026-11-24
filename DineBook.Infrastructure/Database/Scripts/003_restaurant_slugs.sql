-- Restaurant web addresses. Each restaurant gets a slug, for example spice-garden, used as /spice-garden/.
-- Run once on each database. Safe to run again.
-- Slugs are filled in from the restaurant names: lower case, spaces become hyphens, and a few
-- punctuation marks are removed. Duplicates get -2, -3 and so on. Names that match a page
-- name (login, bookings...) get -restaurant. Check the slugs afterwards.

IF COL_LENGTH('Restaurant', 'Slug') IS NULL
    ALTER TABLE Restaurant ADD Slug NVARCHAR(60) NULL;
GO

;WITH Cleaned AS (
    SELECT Id,
           LEFT(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
               LOWER(LTRIM(RTRIM(Name))),
               ' ', '-'), '''', ''), '.', ''), ',', ''), '&', ''), '(', ''), ')', ''), 50) AS Slugged
    FROM Restaurant
    WHERE Slug IS NULL
),
Based AS (
    SELECT Id,
           CASE
               WHEN Slugged = '' THEN 'restaurant'
               WHEN Slugged IN ('admin', 'api', 'bookings', 'checkout', 'css', 'error', 'img', 'index', 'js', 'lib',
                                'login', 'logout', 'orders', 'payment', 'registration', 'reservations')
                    THEN Slugged + '-restaurant'
               ELSE Slugged
           END AS Base
    FROM Cleaned
),
Numbered AS (
    SELECT Id, Base, ROW_NUMBER() OVER (PARTITION BY Base ORDER BY Id) AS Nth
    FROM Based
)
UPDATE r
SET r.Slug = CASE WHEN n.Nth = 1 THEN n.Base ELSE n.Base + '-' + CAST(n.Nth AS NVARCHAR(10)) END
FROM Restaurant r
JOIN Numbered n ON n.Id = r.Id;
GO

IF EXISTS (SELECT 1 FROM Restaurant WHERE Slug IS NULL)
    THROW 50000, 'Some restaurants have no slug. Check their names and run this script again.', 1;
GO

ALTER TABLE Restaurant ALTER COLUMN Slug NVARCHAR(60) NOT NULL;
GO

-- Stops here (with the slugs already filled in) if two slugs are the same. Fix those rows, then run again.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Restaurant_Slug' AND object_id = OBJECT_ID('Restaurant'))
    CREATE UNIQUE INDEX UX_Restaurant_Slug ON Restaurant (Slug);
GO
