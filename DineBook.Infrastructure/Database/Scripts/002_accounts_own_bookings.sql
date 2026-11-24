-- Accounts own bookings. Customer records (one per phone) are no longer used.
-- Run once on each database. Safe to run again.
-- Fails, and changes nothing, when a booking has no account to belong to.
-- Link or remove those bookings first (see the check below).

IF COL_LENGTH('ApplicationUser', 'Phone') IS NULL
    ALTER TABLE ApplicationUser ADD Phone NVARCHAR(15) NOT NULL CONSTRAINT DF_ApplicationUser_Phone DEFAULT ('');
GO

-- Copy the phone number of linked customer records onto their accounts
UPDATE u
SET u.Phone = c.Phone
FROM ApplicationUser u
JOIN Customer c ON c.ApplicationUserId = u.Id
WHERE u.Phone = '';
GO

IF COL_LENGTH('Reservation', 'ApplicationUserId') IS NULL
    ALTER TABLE Reservation ADD ApplicationUserId BIGINT NULL;
GO

-- Bookings made through a linked customer belong to that account
UPDATE r
SET r.ApplicationUserId = c.ApplicationUserId
FROM Reservation r
JOIN Customer c ON c.Id = r.CustomerId
WHERE r.ApplicationUserId IS NULL AND c.ApplicationUserId IS NOT NULL;
GO

-- Stop here if any booking still has no account
IF EXISTS (SELECT 1 FROM Reservation WHERE ApplicationUserId IS NULL)
    THROW 50000, 'Some bookings have no account. Link them to a user (Reservation.ApplicationUserId) or remove them, then run this script again.', 1;
GO

ALTER TABLE Reservation ALTER COLUMN ApplicationUserId BIGINT NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Reservation_ApplicationUser_ApplicationUserId')
    ALTER TABLE Reservation ADD CONSTRAINT FK_Reservation_ApplicationUser_ApplicationUserId
        FOREIGN KEY (ApplicationUserId) REFERENCES ApplicationUser (Id);
GO
