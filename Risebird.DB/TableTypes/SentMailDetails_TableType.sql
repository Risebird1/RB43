IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'SentMailDetails_TableType')
BEGIN
CREATE TYPE [dbo].[SentMailDetails_TableType] AS TABLE(
	[To] [varchar](max) NULL,
	[Cc] [varchar](max) NULL,
	[Bcc] [varchar](max) NULL,
	[MailBody] [varchar](max) NULL,
	[MailSubject] [varchar](500) NULL,
	[sg_message_id] [varchar](50) NULL,
	[IRFId] [int] NULL,
	[InterviewId] [int] NULL,
	[CreatedAt] datetime Null,
	[SentBy] [varchar](100) NULL
)
End
Go