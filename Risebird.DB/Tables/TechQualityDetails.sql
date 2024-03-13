IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TechQualityDetails')
BEGIN 

CREATE TABLE [dbo].[TechQualityDetails](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Interviewid] [int] NULL,
	[CreatedBy] [varchar](150) NULL,
	[CreatedOn] [datetime] NULL,
	[IsActive] [bit] NULL,
	FOREIGN KEY([Interviewid]) REFERENCES [dbo].[InterviewMeetings] ([id]),
 CONSTRAINT [PK_TechQualityDetails] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO



