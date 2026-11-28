-- Booking deposit, partial refunds and deposit-linked payments. Run once on existing databases.
-- Each step checks first, so running the script again does nothing.
-- Table names match the EF mapping: Payment, Reservation, Orders.
-- GO separates the batches: a column added in one batch cannot be used by a constraint in the same batch.

-- 1. Amount applied from a booking deposit to a food order
IF COL_LENGTH('dbo.Payment', 'AdvanceApplied') IS NULL
    ALTER TABLE dbo.Payment ADD AdvanceApplied DECIMAL(18, 2) NOT NULL
        CONSTRAINT DF_Payment_AdvanceApplied DEFAULT (0);
GO

-- 2. Amount refunded so far (cancellation refunds are partial)
IF COL_LENGTH('dbo.Payment', 'RefundedAmount') IS NULL
    ALTER TABLE dbo.Payment ADD RefundedAmount DECIMAL(18, 2) NOT NULL
        CONSTRAINT DF_Payment_RefundedAmount DEFAULT (0);
GO

-- 3. Booking link, so a deposit (which has no food order) can point at its booking
IF COL_LENGTH('dbo.Payment', 'ReservationId') IS NULL
    ALTER TABLE dbo.Payment ADD ReservationId BIGINT NULL;
GO

-- Food payments get their booking from their order
UPDATE p
SET p.ReservationId = o.ReservationId
FROM dbo.Payment p
JOIN dbo.Orders o ON o.Id = p.OrderId
WHERE p.ReservationId IS NULL;
GO

-- 4. Deposits have no food order
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Payment') AND name = N'OrderId' AND is_nullable = 0)
    ALTER TABLE dbo.Payment ALTER COLUMN OrderId BIGINT NULL;
GO

-- 5. Foreign key to the booking
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Payment_Reservation')
    ALTER TABLE dbo.Payment ADD CONSTRAINT FK_Payment_Reservation
        FOREIGN KEY (ReservationId) REFERENCES dbo.Reservation (Id);
GO

-- 6. Every payment belongs to a food order or a booking
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Payment_Target')
    ALTER TABLE dbo.Payment ADD CONSTRAINT CK_Payment_Target
        CHECK (OrderId IS NOT NULL OR ReservationId IS NOT NULL);
GO
