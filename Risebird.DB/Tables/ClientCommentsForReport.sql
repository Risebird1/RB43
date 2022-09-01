IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ClientCommentsForReport')

Begin
	CREATE TABLE [dbo].[ClientCommentsForReport](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewId] [int] NOT NULL,
	[Comment] [varchar](max) NULL,
	[CreatedAt][datetime] NULL,
	[ModifiedAt][datetime] NULL,
	[CreatedBy][varchar](50) NULL,
	[ModifiedBy][varchar](50) NULL,
	CONSTRAINT [FK_ClientCommentsForReport_InterviewId] FOREIGN KEY([InterviewId]) REFERENCES [dbo].[InterviewMeetings] ([id]),
	CONSTRAINT [PK_ClientCommentsForReport] PRIMARY KEY CLUSTERED 
	(
		[id] ASC
	) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
	) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
End

