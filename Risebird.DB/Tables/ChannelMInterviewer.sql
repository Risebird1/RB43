IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ChannelMInterviewer')
BEGIN  
CREATE TABLE [dbo].[ChannelMInterviewer](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[ProcessedDate] [datetime] NULL,
	[Name] [varchar](100) NULL,
	[EmailAddress] [varchar](150) NULL,
	[Mobile] [varchar](14) NULL,
	[CVLink] [varchar](max) NULL,
	[CodedCVLink] [varchar](max) NULL,
	[PrimarySKill] [varchar](50) NULL,
	[Role] [varchar](50) NULL,
	[Priority] [varchar](5) NULL,
	[Comment] [varchar](max) NULL,
	[Approved] [bit] NULL,
	[DMCallAvailability] [bit] NULL,
	[AddedoverPortal] [bit] NULL,
	[AddedoverSKillChannel] [bit] NULL,
	[QC] [bit] NULL,
	[Emergency] [bit] NULL,
	[Training] [bit] NULL,
	[AdvancePayment] [bit] NULL,
	[CallAttended] [bit] NULL,
	[Channelid] [int] NULL,
	[CreatedBy] [int] NULL,
	[Trainingmailsend] [bit] NULL,
	[PerInterviewrate] [int] NULL,
	[TrainingTime] [varchar](20) NULL,
	[CallTime] [varchar](20) NULL,
 CONSTRAINT [PK_ChannelMInterviewer] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO


