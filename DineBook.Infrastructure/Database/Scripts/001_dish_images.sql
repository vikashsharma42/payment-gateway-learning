-- DineBook: dish photos
-- Adds the ImageUrl column to MenuItem and OrderItem and fills in the sample dish photos.
-- Run on the database used by DineBook.Web (SQL Server). Safe to run more than once.
-- Run in SSMS or with: sqlcmd -S <server> -d <database> -E -i 001_dish_images.sql
-- The GO lines are required: SQL Server must see the new column before the UPDATE statements use it.

IF COL_LENGTH('dbo.MenuItem', 'ImageUrl') IS NULL
    ALTER TABLE dbo.MenuItem ADD ImageUrl nvarchar(500) NULL;
GO

IF COL_LENGTH('dbo.OrderItem', 'ImageUrl') IS NULL
    ALTER TABLE dbo.OrderItem ADD ImageUrl nvarchar(500) NULL;
GO

-- Sample dish photos. Names must match DiningSeeder.cs.
UPDATE dbo.MenuItem
SET ImageUrl = CASE Name
        WHEN N'Dal Makhani'  THEN N'https://picsum.photos/seed/dinebook-dal-makhani/400/300'
        WHEN N'Paneer Tikka' THEN N'https://picsum.photos/seed/dinebook-paneer-tikka/400/300'
        WHEN N'Garlic Naan'  THEN N'https://picsum.photos/seed/dinebook-garlic-naan/400/300'
        WHEN N'Sweet Lassi'  THEN N'https://picsum.photos/seed/dinebook-sweet-lassi/400/300'
        WHEN N'Masala Chai'  THEN N'https://picsum.photos/seed/dinebook-masala-chai/400/300'
        WHEN N'Veg Sandwich' THEN N'https://picsum.photos/seed/dinebook-veg-sandwich/400/300'
    END
WHERE ImageUrl IS NULL
  AND Name IN (N'Dal Makhani', N'Paneer Tikka', N'Garlic Naan', N'Sweet Lassi', N'Masala Chai', N'Veg Sandwich');
GO

-- Orders placed before this script ran get the photo of their dish (the photo is copied, as for new orders)
UPDATE oi
SET oi.ImageUrl = mi.ImageUrl
FROM dbo.OrderItem AS oi
INNER JOIN dbo.MenuItem AS mi ON mi.Id = oi.MenuItemId
WHERE oi.ImageUrl IS NULL
  AND mi.ImageUrl IS NOT NULL;
GO

-- Check: every sample dish should show a URL
SELECT Name, ImageUrl FROM dbo.MenuItem ORDER BY Name;
