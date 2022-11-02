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
	[CreatedAt] datetime Null,
	[SentBy] [varchar](100) NULL,
 CONSTRAINT [PK_SentMailDetails] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

End
Else
Begin
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'SentBy')
      AND Object_ID = Object_ID(N'SentMailDetails'))
BEGIN
    ALTER TABLE SentMailDetails
    Add SentBy [varchar](100) NULL
END
End
GO
