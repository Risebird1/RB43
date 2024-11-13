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
	[PaymentComplianceCount] [int] NULL,
	[ModifiedBy] [varchar](150) NULL,
	[ModifiedOn] [datetime] NULL,
	[VideoEnable] [bit] NULL,
	[isQcDone] [bit] NULL,
	[Recommendation] [varchar](15) NULL,
	[IsTechQCDone] [bit] NULL,
    [RecommendationComments] [varchar](500) NULL,
    [InterviewerVideo] [varchar](250) NULL,
 CONSTRAINT [PK_PaymentSLADetails] PRIMARY KEY CLUSTERED 
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
    WHERE Name IN (N'PaymentComplianceCount')
      AND Object_ID = Object_ID(N'PaymentSLADetails'))
BEGIN
    ALTER TABLE PaymentSLADetails
    Add PaymentComplianceCount INT
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'VideoEnable')
      AND Object_ID = Object_ID(N'PaymentSLADetails'))
BEGIN
    ALTER TABLE PaymentSLADetails
    Add VideoEnable bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'isQcDone')
      AND Object_ID = Object_ID(N'PaymentSLADetails'))
BEGIN
    ALTER TABLE PaymentSLADetails
    Add isQcDone bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Recommendation')
      AND Object_ID = Object_ID(N'PaymentSLADetails'))
BEGIN
    ALTER TABLE PaymentSLADetails
    Add Recommendation varchar(15)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsTechQCDone')
      AND Object_ID = Object_ID(N'PaymentSLADetails'))
BEGIN
    ALTER TABLE PaymentSLADetails
    Add IsTechQCDone bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'RecommendationComments')
      AND Object_ID = Object_ID(N'PaymentSLADetails'))
BEGIN
    ALTER TABLE PaymentSLADetails
    Add RecommendationComments varchar(500)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'InterviewerVideo')
      AND Object_ID = Object_ID(N'PaymentSLADetails'))
BEGIN
    ALTER TABLE PaymentSLADetails
    Add InterviewerVideo varchar(250)
END
END
GO


