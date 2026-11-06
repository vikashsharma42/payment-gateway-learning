/* ---------------------------------------------------------------
   Migration: idempotency keys, payment session id, longer ImageUrl
   Run once against DineBook on (localdb)\ProjectModels.
   Safe to re-run: each step checks whether it is already applied.
   --------------------------------------------------------------- */
USE DineBook;
GO

IF COL_LENGTH('dbo.Orders', 'PaymentSessionId') IS NULL
    ALTER TABLE dbo.Orders ADD PaymentSessionId NVARCHAR(200) NULL;
GO

IF COL_LENGTH('dbo.Orders', 'IdempotencyKey') IS NULL
    ALTER TABLE dbo.Orders ADD IdempotencyKey NVARCHAR(64) NULL;
GO

-- Unique only where a key is present, so older rows (NULL key) do not conflict
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Orders_IdempotencyKey' AND object_id = OBJECT_ID('dbo.Orders'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_Orders_IdempotencyKey
        ON dbo.Orders (IdempotencyKey)
        WHERE IdempotencyKey IS NOT NULL;
GO

-- Product image URLs are longer than 50 characters
ALTER TABLE dbo.Products ALTER COLUMN ImageUrl NVARCHAR(500) NOT NULL;
GO

-- Verify
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('Orders', 'Products')
  AND COLUMN_NAME IN ('PaymentSessionId', 'IdempotencyKey', 'ImageUrl');
GO
