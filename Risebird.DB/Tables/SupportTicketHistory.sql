IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'SupportTicketHistory')
BEGIN
    CREATE TABLE [dbo].[SupportTicketHistory] (
        [Id]         INT           IDENTITY(1,1) NOT NULL,
        [TicketId]   INT           NOT NULL,
        [FromStatus] VARCHAR(50)   NULL,
        [ToStatus]   VARCHAR(50)   NOT NULL,
        [ChangedBy]  VARCHAR(150)  NOT NULL,
        [ChangedAt]  DATETIME      NOT NULL CONSTRAINT [DF_SupportTicketHistory_ChangedAt] DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_SupportTicketHistory] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_SupportTicketHistory_Ticket]
            FOREIGN KEY ([TicketId]) REFERENCES [dbo].[SupportTickets] ([Id])
    )

    CREATE NONCLUSTERED INDEX [IX_SupportTicketHistory_TicketId]
        ON [dbo].[SupportTicketHistory] ([TicketId] ASC)
        INCLUDE ([ChangedAt])
END
GO
