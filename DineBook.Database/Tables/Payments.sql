-- One gateway payment attempt for one food order
CREATE TABLE Payments (
    Id               BIGINT IDENTITY(1, 1) NOT NULL,
    OrderId          BIGINT NULL,
    ReservationId    BIGINT NULL,
    PublicId         UNIQUEIDENTIFIER NOT NULL,
    Amount           DECIMAL(18, 2) NOT NULL,
    GatewayOrderId   NVARCHAR(100) NOT NULL,
    PaymentSessionId NVARCHAR(200) NULL,
    IdempotencyKey   NVARCHAR(64) NULL,
    CustomerEmail    NVARCHAR(254) NULL,
    RefundId         NVARCHAR(64) NULL,
    AdvanceApplied   DECIMAL(18, 2) NOT NULL CONSTRAINT DF_Payments_AdvanceApplied DEFAULT (0),
    RefundedAmount   DECIMAL(18, 2) NOT NULL CONSTRAINT DF_Payments_RefundedAmount DEFAULT (0),
    Status           NVARCHAR(20) NOT NULL,
    CreatedDateTime DATETIMEOFFSET NOT NULL CONSTRAINT DF_Payments_CreatedDateTime DEFAULT SYSUTCDATETIME(),
    CreatedBy        NVARCHAR(256) NULL,
    UpdatedDateTime DATETIMEOFFSET NOT NULL CONSTRAINT DF_Payments_UpdatedDateTime DEFAULT SYSUTCDATETIME(),
    UpdatedBy        NVARCHAR(256) NULL,
    CONSTRAINT PK_Payments PRIMARY KEY (Id),
    CONSTRAINT FK_Payments_Orders FOREIGN KEY (OrderId) REFERENCES Orders (Id),
    CONSTRAINT FK_Payments_Reservations FOREIGN KEY (ReservationId) REFERENCES Reservations (Id),
    CONSTRAINT CK_Payments_Target CHECK (OrderId IS NOT NULL OR ReservationId IS NOT NULL),
    CONSTRAINT UQ_Payments_GatewayOrderId UNIQUE (GatewayOrderId),
    CONSTRAINT UQ_Payments_PublicId UNIQUE (PublicId),
    CONSTRAINT CK_Payments_Status CHECK (Status IN ('Pending', 'Paid', 'Failed', 'Refunded'))
);

CREATE NONCLUSTERED INDEX IX_Payments_OrderId ON Payments (OrderId);

CREATE UNIQUE NONCLUSTERED INDEX UX_Payments_IdempotencyKey
    ON Payments (IdempotencyKey)
    WHERE IdempotencyKey IS NOT NULL;
