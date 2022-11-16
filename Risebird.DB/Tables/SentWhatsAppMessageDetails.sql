/****** Object:  Table [dbo].[SentMailDetails]   ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'SentWhatsAppMessageDetails')
BEGIN

CREATE TABLE [dbo].[SentWhatsAppMessageDetails](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Recipient] [varchar](60) NULL,
	[CountryCode] [varchar](5) NULL,
	[WhatsappNumber] [varchar](15) NULL,
	[Message] [varchar](max) NULL,
	[ButtonLink] [varchar](500) NULL,
	[Event] [varchar](500) NULL,
	[IRFId] [int] NULL,
	[InterviewId] [int] NULL,
	[InterviewDate] datetime Null,
	[Header] [varchar](500) NULL,
	[CreatedAt] datetime Null,
	[SentBy] [varchar](60) NULL,
 CONSTRAINT [PK_SentWhatsAppMessageDetails] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

End

GO

