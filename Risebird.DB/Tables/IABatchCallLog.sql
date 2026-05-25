IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'IABatchCallLog')
BEGIN
    CREATE TABLE [dbo].[IABatchCallLog](
        [Id]                     INT IDENTITY(1,1) NOT NULL,
        [MagName]                NVARCHAR(200) NOT NULL,
        [InitiatedBy]            VARCHAR(100) NOT NULL,
        [InitiatedAt]            DATETIME NOT NULL DEFAULT GETUTCDATE(),
        [InterviewersAttempted]  INT NOT NULL DEFAULT 0,
        [InterviewersSucceeded]  INT NOT NULL DEFAULT 0,
        [InterviewersFailed]     INT NOT NULL DEFAULT 0,
        [InterviewersSkipped]    INT NOT NULL DEFAULT 0,
        [CompletedAt]            DATETIME NULL,
        [Status]                 VARCHAR(20) NOT NULL DEFAULT 'Running',
        CONSTRAINT [PK_IABatchCallLog] PRIMARY KEY CLUSTERED ([Id] ASC)
    ) ON [PRIMARY]
END
GO
