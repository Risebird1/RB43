IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewReportHistory')

BEGIN

CREATE TABLE [dbo].[InterviewReportHistory](
	[id] [int] NOT NULL,
	[IRFId] [int] NOT NULL,
	[InterviewId] [int] NOT NULL,
	[CandidateName] [nvarchar](100) NULL,
	[confirmationStatus] [nvarchar](100) NULL,
	[OverallScore] [float] NULL,
	[Rating] [int] NULL,
	[TechSkillComments] [varchar](max) NULL,
	[SkillGeneralComments] [varchar](max) NULL,
	[JoinabilityComments] [varchar](max) NULL,
	[SelectabilityComments] [varchar](max) NULL,
	[ProctoringCount] [int] NULL,
	[CreatedOn] [datetime] NULL,
	[ModifiedOn] [datetime] NULL,
	[CreatedBy] [varchar](100) NULL,
	[ModifiedBy] [varchar](100) NULL,
	[PerformedAt][datetime] NULL,
	[PerformedBy][varchar](100) NULL
)

END

GO





