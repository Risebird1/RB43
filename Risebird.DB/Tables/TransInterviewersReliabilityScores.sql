IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInterviewersReliabilityScores')
BEGIN  
CREATE TABLE [dbo].[TransInterviewersReliabilityScores](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewerId] [int] NOT NULL,
	[InterviewMeetingId] [int] NULL,
	[ReliabilityScoreTypeId] [int] NOT NULL,
	[Point] [int] NOT NULL,
	[CreatedDate] [datetime] NULL,
	FOREIGN KEY([InterviewerId]) REFERENCES [dbo].[InterviewerProfile] ([Id]),
	FOREIGN KEY([ReliabilityScoreTypeId]) REFERENCES [dbo].[ReliabilityScoreTypes] ([id]),
 CONSTRAINT [PK_TransInterviewersReliabilityScores] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


