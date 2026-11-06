CREATE TABLE UserRefreshTokens (
    Id                  BIGINT IDENTITY(1, 1) NOT NULL,
    TokenHash           NVARCHAR(64) NOT NULL,
    ApplicationUserId   BIGINT NOT NULL,
    CreatedAtUtc        DATETIMEOFFSET NOT NULL,
    ExpiresAtUtc        DATETIMEOFFSET NOT NULL,
    RevokedAtUtc        DATETIMEOFFSET NULL,
    RevokeReason        NVARCHAR(20) NULL,
    ReplacedByTokenHash NVARCHAR(64) NULL,
    CONSTRAINT PK_UserRefreshTokens PRIMARY KEY (Id),
    CONSTRAINT UQ_UserRefreshTokens_TokenHash UNIQUE (TokenHash),
    -- Deleting a user removes their sessions
    CONSTRAINT FK_UserRefreshTokens_Users FOREIGN KEY (ApplicationUserId) REFERENCES Users (Id) ON DELETE CASCADE,
    CONSTRAINT CK_UserRefreshTokens_RevokeReason CHECK (
        RevokeReason IS NULL
        OR RevokeReason IN ('Rotated', 'LoggedOut', 'SessionReplaced', 'AdminRevoked', 'ReuseDetected'))
);

CREATE NONCLUSTERED INDEX IX_UserRefreshTokens_ApplicationUserId ON UserRefreshTokens (ApplicationUserId);
