IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'CodeProjectInfraSetupRuns')
BEGIN
    CREATE TABLE [dbo].[CodeProjectInfraSetupRuns](
        [Id]             [int] IDENTITY(1,1) NOT NULL,
        [Status]         [varchar](20)   NOT NULL,   -- running | completed | failed | blocked
        [StateJson]      [nvarchar](max) NULL,
        [StartedBy]      [varchar](150)  NULL,
        [StartedAtUtc]   [datetime2]     NOT NULL CONSTRAINT DF_CodeProjectInfraSetupRuns_Started DEFAULT SYSUTCDATETIME(),
        [UpdatedAtUtc]   [datetime2]     NOT NULL CONSTRAINT DF_CodeProjectInfraSetupRuns_Updated DEFAULT SYSUTCDATETIME(),
        [CompletedAtUtc] [datetime2]     NULL,
        [LeaseUntilUtc]  [datetime2]     NULL,
     CONSTRAINT [PK_CodeProjectInfraSetupRuns] PRIMARY KEY CLUSTERED ([Id] ASC)
    ) ON [PRIMARY];
END
GO
