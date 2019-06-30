IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'Report')
BEGIN  
CREATE TABLE [dbo].[Report](					
			[id] [int] IDENTITY(1,1) NOT NULL,
			[idMeeting] [int] NOT NULL,	
			[VideoLink] VARCHAR(MAX) NULL,
			[VideoDescription] VARCHAR(MAX) NULL,
			[ReportLink] VARCHAR(MAX) NULL,
			[ReportDescription] VARCHAR(MAX) NULL,
			[OverAllRating] [Real] NULL,
			CONSTRAINT [FK_Meeting_Report] FOREIGN KEY (idMeeting) REFERENCES InterviewMeetings(id),
 CONSTRAINT [PK_Report] PRIMARY KEY CLUSTERED 					
(					
	[id] ASC				
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)					
)					

END

GO
