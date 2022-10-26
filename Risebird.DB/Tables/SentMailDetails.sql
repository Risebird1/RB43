/****** Object:  Table [dbo].[SentMailDetails]   ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'SentMailDetails')
BEGIN

CREATE TABLE [dbo].[SentMailDetails](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[To] [varchar](max) NULL,
	[Cc] [varchar](max) NULL,
	[Bcc] [varchar](max) NULL,
	[MailBody] [varchar](max) NULL,
	[MailSubject] [varchar](500) NULL,
	[sg_message_id] [varchar](50) NULL,
	[IRFId] [int] NULL,
	[InterviewId] [int] NULL,
	[CreatedAt] datetime Null
 CONSTRAINT [PK_SentMailDetails] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

End
GO
