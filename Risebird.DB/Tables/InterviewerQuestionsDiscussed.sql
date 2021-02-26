/****** Object:  Table [dbo].[InterviewerQuestionsDiscussed]    Script Date: 22-01-2021 3.20.35 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewerQuestionsDiscussed')

Begin
	CREATE TABLE [dbo].[InterviewerQuestionsDiscussed](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewId] [int] NOT NULL,
	[Question] [varchar](max) NOT NULL,
	[RatingId] [int] NOT NULL,
	[Timing] [int]
	 CONSTRAINT [PK_InterviewerQuestionsDiscussed] PRIMARY KEY CLUSTERED 
	(
		[id] ASC
	)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
	) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

End
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Timing')
      AND Object_ID = Object_ID(N'InterviewerQuestionsDiscussed'))
BEGIN
    ALTER TABLE InterviewerQuestionsDiscussed
    Add Timing INT NOT NULL DEFAULT 0
END

GO




