/****** Object:  Table [dbo].[CallRecordingData]   ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'CallRecordingData')
BEGIN

CREATE TABLE [dbo].[CallRecordingData](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewId] [int] NULL,
	[IRFId] [varchar](50) NULL,
	[RecordingSid] [varchar](1000) NULL,
	[CallSid] [varchar](50) NULL,
	[CallRecipient] [varchar](150) NULL,
	[Caller] [varchar](150) NULL,
	[RecipientProfileType] [varchar](20) NULL,
	[DateCreated] [datetime] NULL,
	[StartTime][datetime] NULL,
	[Price] [varchar](10) NULL,
	[PriceUnit] [varchar](10) NULL,
	[Duration] [varchar](10) NULL,
	[Status] [varchar](20) NULL,
	[RecipientPhoneNumber] nvarchar(20) NULL,
	[RecipientCountryCode] nvarchar(5) NULL,
	[RecordingUri] [varchar](1000) NULL,
 CONSTRAINT [PK_CallRecordingData] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

End
GO


