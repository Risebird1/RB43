IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'NaukriBatchCallLog')
BEGIN
    CREATE TABLE [dbo].[NaukriBatchCallLog](
        [Id]                 INT IDENTITY(1,1) NOT NULL,
        [MagName]            NVARCHAR(200) NOT NULL,
        [InitiatedBy]        VARCHAR(100) NOT NULL,
        [InitiatedAt]        DATETIME NOT NULL DEFAULT GETUTCDATE(),
        [ProspectsAttempted]  INT NOT NULL DEFAULT 0,
        [ProspectsSucceeded]  INT NOT NULL DEFAULT 0,
        [ProspectsFailed]     INT NOT NULL DEFAULT 0,
        [ProspectsSkipped]    INT NOT NULL DEFAULT 0,
        [CompletedAt]         DATETIME NULL,
        [Status]              VARCHAR(20) NOT NULL DEFAULT 'Running',
        CONSTRAINT [PK_NaukriBatchCallLog] PRIMARY KEY CLUSTERED ([Id] ASC)
    ) ON [PRIMARY]
END
GO
