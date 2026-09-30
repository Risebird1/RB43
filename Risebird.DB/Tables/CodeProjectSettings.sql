IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'CodeProjectSettings')
BEGIN
    CREATE TABLE [dbo].[CodeProjectSettings](
        [Name]         [varchar](150)  NOT NULL,   -- same path as the configuration key, e.g. CodeProjects:Azure:SubnetId
        [Value]        [nvarchar](max) NULL,       -- encrypted with IEncryptionHelper when IsSecret = 1
        [IsSecret]     [bit]           NOT NULL CONSTRAINT DF_CodeProjectSettings_IsSecret DEFAULT 0,
        [UpdatedAtUtc] [datetime2]     NOT NULL CONSTRAINT DF_CodeProjectSettings_Updated DEFAULT SYSUTCDATETIME(),
     CONSTRAINT [PK_CodeProjectSettings] PRIMARY KEY CLUSTERED ([Name] ASC)
    ) ON [PRIMARY];
END
GO
