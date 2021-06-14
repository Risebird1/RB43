IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewRatings')
BEGIN  

CREATE TABLE [dbo].[InterviewRatings](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Interviewid] [int] NULL,
	[Userid] [int] NULL,
	[Comment] [varchar](400) NULL,
	[Addedon] [datetime] NULL,
	[StarRating] [int] NULL,
 CONSTRAINT [PK_InterviewRatings] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


