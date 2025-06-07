CREATE TABLE [dbo].[ChatHistory]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [Message] NVARCHAR(MAX) NOT NULL,
    [Role] NVARCHAR(50) NOT NULL,  -- 'user' or 'assistant'
    [Timestamp] DATETIME2 NOT NULL,
    [CreatedOn] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT [CK_ChatHistory_Role] CHECK ([Role] IN ('user', 'assistant'))
)
GO

CREATE INDEX [IX_ChatHistory_UserId] ON [dbo].[ChatHistory] ([UserId])
GO

CREATE INDEX [IX_ChatHistory_Timestamp] ON [dbo].[ChatHistory] ([Timestamp])
GO 