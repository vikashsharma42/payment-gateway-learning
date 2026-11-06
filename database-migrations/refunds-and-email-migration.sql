/* ---------------------------------------------------------------
   Migration: customer email, refund id, and the Refunded status.
   Run once against DineBook on (localdb)\ProjectModels, after
   idempotency-migration.sql. Safe to re-run.
   --------------------------------------------------------------- */
USE DineBook;
GO

IF COL_LENGTH('dbo.Orders', 'CustomerEmail') IS NULL
    ALTER TABLE dbo.Orders ADD CustomerEmail NVARCHAR(254) NULL;
GO

IF COL_LENGTH('dbo.Orders', 'RefundId') IS NULL
    ALTER TABLE dbo.Orders ADD RefundId NVARCHAR(64) NULL;
GO

-- Replace the status check so it also allows 'Refunded'
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Orders_Status')
    ALTER TABLE dbo.Orders DROP CONSTRAINT CK_Orders_Status;
GO

ALTER TABLE dbo.Orders ADD CONSTRAINT CK_Orders_Status
    CHECK (Status IN ('Pending', 'Paid', 'Failed', 'Refunded'));
GO

-- Verify
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME IN ('CustomerEmail', 'RefundId');
GO
