-- Security events: suspected attacks, failed sign-ins and refused requests (risk scoring and the admin security log)
CREATE TABLE SecurityEvent (
    Id          BIGINT IDENTITY(1, 1) NOT NULL,
    OccurredAt  DATETIMEOFFSET NOT NULL CONSTRAINT DF_SecurityEvents_OccurredAt DEFAULT SYSUTCDATETIME(),
    Kind        NVARCHAR(40) NOT NULL,
    Points      INT NOT NULL,
    IpAddress   NVARCHAR(45) NOT NULL,
    UserAgent   NVARCHAR(300) NOT NULL,
    Method      NVARCHAR(10) NOT NULL,
    Path        NVARCHAR(300) NOT NULL,
    Subject     NVARCHAR(254) NOT NULL,
    Detail      NVARCHAR(500) NOT NULL,
    Blocked     BIT NOT NULL,
    CONSTRAINT PK_SecurityEvents PRIMARY KEY (Id)
);

CREATE NONCLUSTERED INDEX IX_SecurityEvents_IpAddress_OccurredAt ON SecurityEvents (IpAddress, OccurredAt);
CREATE NONCLUSTERED INDEX IX_SecurityEvents_Kind_Subject_OccurredAt ON SecurityEvents (Kind, Subject, OccurredAt);
CREATE NONCLUSTERED INDEX IX_SecurityEvents_OccurredAt ON SecurityEvents (OccurredAt);
