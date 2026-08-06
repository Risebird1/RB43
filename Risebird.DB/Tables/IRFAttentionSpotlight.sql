-- IRF Attention Spotlight (manual pin). Soft-unpin via Active=0; one active row per IRF.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'IRFAttentionSpotlight')
BEGIN
    CREATE TABLE dbo.IRFAttentionSpotlight(
        id        INT IDENTITY(1,1) PRIMARY KEY,
        IrfId     INT NOT NULL,
        PinnedBy  VARCHAR(150) NULL,
        PinnedAt  DATETIME NOT NULL CONSTRAINT DF_IRFAttentionSpotlight_PinnedAt DEFAULT (GETDATE()),
        Active    BIT NOT NULL CONSTRAINT DF_IRFAttentionSpotlight_Active DEFAULT (1)
    );
END
GO

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'IRFAttentionSpotlight')
   AND NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'UX_IRFAttentionSpotlight_ActiveIrf'
          AND object_id = OBJECT_ID(N'dbo.IRFAttentionSpotlight')
   )
BEGIN
    CREATE UNIQUE INDEX UX_IRFAttentionSpotlight_ActiveIrf
        ON dbo.IRFAttentionSpotlight (IrfId)
        WHERE Active = 1;
END
GO
