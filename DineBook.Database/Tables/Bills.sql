CREATE TABLE Bills (
    Id            BIGINT IDENTITY(1, 1) NOT NULL,
    ReservationId BIGINT NOT NULL,
    RestaurantId  BIGINT NOT NULL,
    Amount        DECIMAL(18, 2) NOT NULL,
    Status        NVARCHAR(20) NOT NULL,
    CreatedDateTime     DATETIMEOFFSET NOT NULL,
    UpdatedDateTime     DATETIMEOFFSET NOT NULL CONSTRAINT DF_Bills_UpdatedDateTime DEFAULT SYSUTCDATETIME(),
    CreatedBy         NVARCHAR(256) NULL,
    UpdatedBy         NVARCHAR(256) NULL,
    CONSTRAINT PK_Bills PRIMARY KEY (Id),
    -- Bills are financial records: never removed with a reservation or a restaurant
    CONSTRAINT FK_Bills_Reservations FOREIGN KEY (ReservationId) REFERENCES Reservations (Id),
    CONSTRAINT FK_Bills_Restaurants FOREIGN KEY (RestaurantId) REFERENCES Restaurants (Id),
    CONSTRAINT CK_Bills_Amount CHECK (Amount > 0),
    CONSTRAINT CK_Bills_Status CHECK (Status IN ('Pending', 'Paid', 'Failed', 'Refunded'))
);

CREATE NONCLUSTERED INDEX IX_Bills_ReservationId ON Bills (ReservationId);
