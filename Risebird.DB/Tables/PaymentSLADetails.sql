IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'PaymentSLADetails')
Begin
CREATE TABLE [dbo].[PaymentSLADetails](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Interviewid] [int] NULL,
	[Inaccuratedecisionmaking] [varchar](250) NULL,
	[TimeManagement] [varchar](250) NULL,
	[QuestionsRelevance] [varchar](250) NULL,
	[ScoreRubricIgnorance] [varchar](250) NULL,
	[PoorFeedbackComments] [varchar](250) NULL,
	[CandidateInteractionQuality] [varchar](250) NULL,
	[LimitedHandoncoding] [varchar](250) NULL,
	[WrongStatusUpdates] [varchar](250) NULL,
	[WeakInfrastructure] [varchar](250) NULL,
	[ProctoringIgnorance] [varchar](250) NULL,
	[UnethicalPractices] [varchar](250) NULL,
	[PoorCommunicationSkills] [varchar](250) NULL,
	[CreatedBy] [varchar](150) NULL,
	[CreatedOn] [datetime] NULL,
	[PaymentDeduction] [int] NULL,
	[IsApproved] [int] NULL,
	[ModifiedBy] [varchar](150) NULL,
	[ModifiedOn] [datetime] NULL,
 CONSTRAINT [PK_PaymentSLADetails] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


