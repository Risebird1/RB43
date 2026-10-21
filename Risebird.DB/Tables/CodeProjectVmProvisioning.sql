IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'CodeProjectVmProvisioning')
BEGIN
    CREATE TABLE [dbo].[CodeProjectVmProvisioning](
        [Handle]                     [varchar](64)    NOT NULL,
        [SessionId]                  [varchar](50)    NOT NULL,
        [QuestionId]                 [int]            NOT NULL,
        [VMPropertyId]               [int]            NULL,
        [ResourceGroupName]          [varchar](90)    NOT NULL,
        [Location]                   [varchar](50)    NOT NULL,
        [VmName]                     [varchar](64)    NOT NULL,
        [NicName]                    [varchar](80)    NOT NULL,
        [StackImageCode]             [varchar](50)    NULL,
        [GalleryImageId]             [varchar](500)   NOT NULL,
        [OsType]                     [varchar](25)    NULL,
        -- requested | nic | vm | setup | link | ready | failed | released
        [Step]                       [varchar](20)    NOT NULL,
        [Message]                    [nvarchar](1000) NULL,
        -- Short-lived SAS of the candidate zip. Cleared once setup has run.
        [ProjectZipUrl]              [nvarchar](2000) NULL,
        [ReadOnlyPathsJson]          [nvarchar](max)  NULL,
        [CandidateUserName]          [varchar](50)    NOT NULL,
        -- Both passwords encrypted with IEncryptionHelper. The admin password is kept only so a
        -- retried VM PUT is identical (Azure rejects a changed adminPassword on an existing VM).
        [CandidatePasswordProtected] [nvarchar](500)  NOT NULL,
        [AdminPasswordProtected]     [nvarchar](500)  NOT NULL,
        [ConnectUrl]                 [nvarchar](1000) NULL,
        [LeaseUntilUtc]              [datetime2]      NULL,
        [CreatedAtUtc]               [datetime2]      NOT NULL CONSTRAINT DF_CodeProjectVmProvisioning_Created DEFAULT SYSUTCDATETIME(),
        [UpdatedAtUtc]               [datetime2]      NOT NULL CONSTRAINT DF_CodeProjectVmProvisioning_Updated DEFAULT SYSUTCDATETIME(),
        [ReadyAtUtc]                 [datetime2]      NULL,
        [ReleasedAtUtc]              [datetime2]      NULL,
     CONSTRAINT [PK_CodeProjectVmProvisioning] PRIMARY KEY CLUSTERED ([Handle] ASC),
     CONSTRAINT [UQ_CodeProjectVmProvisioning_SessionQuestion] UNIQUE ([SessionId], [QuestionId])
    ) ON [PRIMARY];
END
GO

-- Session integrity: deadline lock and Bastion session monitoring
IF COL_LENGTH('dbo.CodeProjectVmProvisioning', 'DeadlineUtc') IS NULL
    ALTER TABLE CodeProjectVmProvisioning ADD DeadlineUtc datetime2 NULL;
GO
IF COL_LENGTH('dbo.CodeProjectVmProvisioning', 'LockedAtUtc') IS NULL
    ALTER TABLE CodeProjectVmProvisioning ADD LockedAtUtc datetime2 NULL;
GO
IF COL_LENGTH('dbo.CodeProjectVmProvisioning', 'SeenSessionIdsJson') IS NULL
    ALTER TABLE CodeProjectVmProvisioning ADD SeenSessionIdsJson nvarchar(max) NULL;
GO
IF COL_LENGTH('dbo.CodeProjectVmProvisioning', 'IntegrityFlagsJson') IS NULL
    ALTER TABLE CodeProjectVmProvisioning ADD IntegrityFlagsJson nvarchar(max) NULL;
GO
