IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'PaymentSLADetailsHistory')
Begin
CREATE TABLE [dbo].[PaymentSLADetailsHistory](
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
	[PaymentComplianceCount] [int] NULL,
	[Recommendation] [varchar](15) NULL,
	[IsTechQCDone] [bit] NULL,
 CONSTRAINT [PK_PaymentSLADetailsHistory] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
ELSE
BEGIN
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Recommendation')
      AND Object_ID = Object_ID(N'PaymentSLADetailsHistory'))
BEGIN
    ALTER TABLE PaymentSLADetailsHistory
    Add Recommendation varchar(15)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsTechQCDone')
      AND Object_ID = Object_ID(N'PaymentSLADetailsHistory'))
BEGIN
    ALTER TABLE PaymentSLADetailsHistory
    Add IsTechQCDone bit
END
END
GO