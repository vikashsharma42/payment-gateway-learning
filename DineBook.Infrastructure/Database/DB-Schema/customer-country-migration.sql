-- Customer country and phone format. Run once on an existing database (new databases get the column from ddd-domain-migration.sql).
-- 1. Adds ApplicationUser.CountryCode (ISO alpha-2). Existing accounts are set to IN.
-- 2. Customer phones become dial code + national number (919876543210), so the same number always has the same stored form.
--    Only 10-digit customer phones are changed (they were Indian numbers typed without +91).
-- Check the first query before you run step 2. Phones of other lengths are left as they are; fix them by hand.

IF COL_LENGTH('dbo.ApplicationUser', 'CountryCode') IS NULL
    ALTER TABLE dbo.ApplicationUser ADD CountryCode NVARCHAR(2) NOT NULL
        CONSTRAINT DF_ApplicationUser_CountryCode DEFAULT (N'IN');
GO

-- Review: customer phones that do not fit the IN rule (10 digits, or 12 starting with 91)
SELECT Id, RestaurantId, Phone, LEN(Phone) AS Digits
FROM dbo.ApplicationUser
WHERE Role = N'Customer' AND Phone IS NOT NULL
  AND NOT (LEN(Phone) = 10 OR (LEN(Phone) = 12 AND LEFT(Phone, 2) = N'91'));

-- Step 2: add the 91 dial code to 10-digit Indian customer phones
UPDATE dbo.ApplicationUser
SET Phone = N'91' + Phone
WHERE Role = N'Customer' AND CountryCode = N'IN' AND Phone IS NOT NULL AND LEN(Phone) = 10;
GO
