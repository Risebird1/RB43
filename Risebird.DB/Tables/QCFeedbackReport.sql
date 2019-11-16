/****** Object:  Table [dbo].[QCFeedbackReport]    Script Date: 14-11-2019 4.23.15 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'QCFeedbackReport')
BEGIN

CREATE TABLE [dbo].[QCFeedbackReport](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewId] [int] NOT NULL,
	[FraudLipSync] bit default 0,
	[QuestionsRelevance] bit,
	[HandsOnCoding] bit default 0,
	[BiasAndCandidateHandling] bit default 0,
	[TechnicalCommunicationSkills] bit default 0,
	[ChapteringCompleted] bit default 0,
	[QuestionsFilled] bit default 0,
	[TechSkillsMetric] bit default 0,
	[SelectabilityMetric] bit default 0,
	[JoinabilityMetric] bit default 0,
	[GeneralSkillMetric] bit default 0,
	[CommentsFilled] bit default 0,
	[MeaningfulAssessments] bit default 0,
	[CorrectEnglishGrammar] bit default 0,
	[ProctoringReportFilled] bit default 0,
	FOREIGN KEY([InterviewId]) REFERENCES [dbo].[InterviewMeetings] ([id]),
 CONSTRAINT [PK_QCFeedbackReport] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

End
GO


