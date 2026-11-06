-- Payments now belong to a food order (Orders), not to a Bill.
-- The Bills table is removed. The amount of a payment is the order total at payment time.
-- Run once on a database that still has Bills and Payments.BillId.
-- Existing payment rows cannot be mapped to orders automatically, so the migration stops if there are any.

IF EXISTS (SELECT 1 FROM dbo.Payments)
    THROW 50000, 'Payments has rows. Map them to Orders by hand before running this migration.', 1;

IF COL_LENGTH('dbo.Payments', 'BillId') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Payments_Bills')
        ALTER TABLE dbo.Payments DROP CONSTRAINT FK_Payments_Bills;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payments_BillId')
        DROP INDEX IX_Payments_BillId ON dbo.Payments;

    ALTER TABLE dbo.Payments DROP COLUMN BillId;
END;

IF COL_LENGTH('dbo.Payments', 'OrderId') IS NULL
    ALTER TABLE dbo.Payments ADD OrderId BIGINT NOT NULL CONSTRAINT DF_Payments_OrderId_Temp DEFAULT 0;

-- The temporary default only satisfies NOT NULL on an empty table; remove it again
IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_Payments_OrderId_Temp')
    ALTER TABLE dbo.Payments DROP CONSTRAINT DF_Payments_OrderId_Temp;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Payments_Orders')
    ALTER TABLE dbo.Payments ADD CONSTRAINT FK_Payments_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders (Id);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payments_OrderId')
    CREATE NONCLUSTERED INDEX IX_Payments_OrderId ON dbo.Payments (OrderId);

IF OBJECT_ID('dbo.Bills', 'U') IS NOT NULL
    DROP TABLE dbo.Bills;
