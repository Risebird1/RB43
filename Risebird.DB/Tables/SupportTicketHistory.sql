IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'SupportTicketHistory')
BEGIN
    CREATE TABLE [dbo].[SupportTicketHistory] (
        [Id]         INT           IDENTITY(1,1) NOT NULL,
        [TicketId]   INT           NOT NULL,
        [FromStatus] VARCHAR(150)  NULL,
        [ToStatus]   VARCHAR(150)  NOT NULL,
        [ChangedBy]  VARCHAR(150)  NOT NULL,
        [ChangedAt]  DATETIME      NOT NULL CONSTRAINT [DF_SupportTicketHistory_ChangedAt] DEFAULT (GETUTCDATE()),
        [EventType]  VARCHAR(50)   NOT NULL CONSTRAINT [DF_SupportTicketHistory_EventType]  DEFAULT 'StatusChange',
        CONSTRAINT [PK_SupportTicketHistory] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_SupportTicketHistory_Ticket]
            FOREIGN KEY ([TicketId]) REFERENCES [dbo].[SupportTickets] ([Id])
    )

    CREATE NONCLUSTERED INDEX [IX_SupportTicketHistory_TicketId]
        ON [dbo].[SupportTicketHistory] ([TicketId] ASC)
        INCLUDE ([ChangedAt])
END
ELSE
BEGIN
    -- Widen FromStatus / ToStatus so they can hold email addresses (was VARCHAR(50))
    IF EXISTS (SELECT * FROM sys.columns WHERE Name = N'FromStatus' AND Object_ID = Object_ID(N'SupportTicketHistory') AND max_length < 150)
        ALTER TABLE dbo.SupportTicketHistory ALTER COLUMN [FromStatus] VARCHAR(150) NULL;

    IF EXISTS (SELECT * FROM sys.columns WHERE Name = N'ToStatus' AND Object_ID = Object_ID(N'SupportTicketHistory') AND max_length < 150)
        ALTER TABLE dbo.SupportTicketHistory ALTER COLUMN [ToStatus] VARCHAR(150) NOT NULL;

    -- Add EventType column (distinguishes StatusChange from AssignmentChange)
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'EventType' AND Object_ID = Object_ID(N'SupportTicketHistory'))
        ALTER TABLE dbo.SupportTicketHistory
        ADD [EventType] VARCHAR(50) NOT NULL CONSTRAINT [DF_SupportTicketHistory_EventType] DEFAULT 'StatusChange';
END
GO
