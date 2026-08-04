IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'WeekendDriveSpotlight')
BEGIN
    CREATE TABLE WeekendDriveSpotlight(
        id            INT IDENTITY(1,1) PRIMARY KEY,
        IRFId         INT NOT NULL,
        WeekendBucket VARCHAR(20) NOT NULL,   -- 'This' | 'Next'
        ActionsTaken  VARCHAR(300) NULL,      -- comma list of action keys
        CreatedAt DATETIME NULL,
        CreatedBy VARCHAR(150) NULL,
        ModifiedAt DATETIME NULL,
        ModifiedBy VARCHAR(150) NULL,
        CONSTRAINT UQ_WeekendDriveSpotlight_IRF_Bucket UNIQUE (IRFId, WeekendBucket)
    );
END
GO

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'WeekendDriveSpotlight')
   AND NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'UQ_WeekendDriveSpotlight_IRF_Bucket'
          AND object_id = OBJECT_ID(N'dbo.WeekendDriveSpotlight')
   )
BEGIN
    CREATE UNIQUE INDEX UQ_WeekendDriveSpotlight_IRF_Bucket
        ON dbo.WeekendDriveSpotlight (IRFId, WeekendBucket);
END
GO
