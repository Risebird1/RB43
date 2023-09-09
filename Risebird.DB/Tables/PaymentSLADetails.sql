IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'PaymentSLADetails')
Begin
CREATE TABLE [dbo].[PaymentSLADetails](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Interviewid] [int] NULL,
	[Inaccuratedecisionmaking] [varchar](50) NULL,
	[TimeManagement] [varchar](50) NULL,
	[QuestionsRelevance] [varchar](50) NULL,
	[ScoreRubricIgnorance] [varchar](50) NULL,
	[PoorFeedbackComments] [varchar](50) NULL,
	[CandidateInteractionQuality] [varchar](50) NULL,
	[LimitedHandoncoding] [varchar](50) NULL,
	[WrongStatusUpdates] [varchar](50) NULL,
	[WeakInfrastructure] [varchar](50) NULL,
	[ProctoringIgnorance] [varchar](50) NULL,
	[UnethicalPractices] [varchar](50) NULL,
	[PoorCommunicationSkills] [varchar](50) NULL,
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


