IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'TdsLapseReminderLog')
BEGIN
CREATE TABLE [dbo].[TdsLapseReminderLog](
    [id]                INT           IDENTITY(1,1) NOT NULL,
    [Userid]            INT           NOT NULL,
    [PaymentReference]  NVARCHAR(100) NOT NULL,
    [ReminderIndex]     INT           NOT NULL,
    [SentOn]            DATETIME      NOT NULL CONSTRAINT DF_TdsLapseReminderLog_SentOn DEFAULT (GETDATE()),
    [EmailTo]           NVARCHAR(200) NULL,
    CONSTRAINT [PK_TdsLapseReminderLog] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
END
GO
