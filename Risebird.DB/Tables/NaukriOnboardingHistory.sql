IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'NaukriOnboardingHistory')
BEGIN
    CREATE TABLE [dbo].[NaukriOnboardingHistory] (
        [Id]         INT           IDENTITY(1,1) NOT NULL,
        [RecordId]   INT           NOT NULL,
        [EventType]  VARCHAR(50)   NOT NULL DEFAULT 'StatusChange',
        [FromValue]  NVARCHAR(500) NULL,
        [ToValue]    NVARCHAR(500) NULL,
        [ChangedBy]  VARCHAR(150)  NOT NULL,
        [ChangedAt]  DATETIME      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_NaukriOnboardingHistory] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_NaukriOnboardingHistory_Record]
            FOREIGN KEY ([RecordId]) REFERENCES [dbo].[NaukriProspectiveInterviewer] ([Id])
    )

    CREATE NONCLUSTERED INDEX [IX_NaukriOnboardingHistory_RecordId]
        ON [dbo].[NaukriOnboardingHistory] ([RecordId] ASC)
        INCLUDE ([ChangedAt])
END
GO
