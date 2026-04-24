IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'InterviewerEmailAvailabilityHistory')
BEGIN
    CREATE TABLE [dbo].[InterviewerEmailAvailabilityHistory] (
        [Id]         INT           IDENTITY(1,1) NOT NULL,
        [RecordId]   INT           NOT NULL,
        [EventType]  VARCHAR(50)   NOT NULL DEFAULT 'StatusChange',
        [FromValue]  NVARCHAR(500) NULL,
        [ToValue]    NVARCHAR(500) NULL,
        [ChangedBy]  VARCHAR(150)  NOT NULL,
        [ChangedAt]  DATETIME      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_IEAHistory] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_IEAHistory_Record]
            FOREIGN KEY ([RecordId]) REFERENCES [dbo].[InterviewerEmailAvailability] ([Id])
    )

    CREATE NONCLUSTERED INDEX [IX_IEAHistory_RecordId]
        ON [dbo].[InterviewerEmailAvailabilityHistory] ([RecordId] ASC)
        INCLUDE ([ChangedAt])
END
GO
