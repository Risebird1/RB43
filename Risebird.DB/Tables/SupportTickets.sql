IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'SupportTickets')
BEGIN
    CREATE TABLE [dbo].[SupportTickets] (
        [Id]           INT            IDENTITY(1,1) NOT NULL,
        [SenderEmail]  VARCHAR(150)   NOT NULL,
        [Subject]      NVARCHAR(500)  NULL,
        [MailContent]  NVARCHAR(MAX)  NULL,
        [ReceivedAt]   DATETIME       NULL,
        [Intent]       NVARCHAR(500)  NULL,
        [UserId]       INT            NULL,
        [ClientId]     INT            NULL,
        [Status]       VARCHAR(50)    NOT NULL CONSTRAINT [DF_SupportTickets_Status] DEFAULT ('Open'),
        [CreatedAt]    DATETIME       NOT NULL CONSTRAINT [DF_SupportTickets_CreatedAt] DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_SupportTickets] PRIMARY KEY CLUSTERED ([Id] ASC)
            WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF,
                  ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
    )
END
GO
