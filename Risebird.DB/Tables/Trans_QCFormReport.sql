IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'Trans_QCFormReport')
BEGIN
CREATE TABLE [dbo].[Trans_QCFormReport](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Interviewid] [int] NULL,
	[Userid] [int] NULL,
	[IsReportBlocked] [bit] NULL,
	[IsCodingPresent] [bit] NULL,
	[IsVideoClearQuality] [bit] NULL,
	[IsInterviewerCommunicationClear] [bit] NULL,
	[IsSufficientVideoDuration] [bit] NULL,
	[IsInterviewStructured] [bit] NULL,
	[IsScoringrubriccorrect] [bit] NULL,
	[IsQuestionswithtime] [bit] NULL,
	[IsCommentfilled] [bit] NULL,
	[IsMeetingStatusfilled] [bit] NULL,
	[IsRelevancequestions] [bit] NULL,
	[IsDelayresuminginterview] [bit] NULL,
	[IsBackgroundnoise] [bit] NULL,
	[IsPoorinterviewerinfrastructure] [bit] NULL,
	[IsPoorhandlingcandidatequery] [bit] NULL,
	[IsPersonalquestions] [bit] NULL,
	[IsAugmenting] [bit] NULL,
	[IsBiasedView] [bit] NULL,
	[IsLettingcandidatespeak] [bit] NULL,
	[IsSuspiciousappfound] [bit] NULL,
	[IsLipsyncissue] [bit] NULL,
	[Addedon] [datetime] NULL,
	[Redflags] [int] NULL,
	[ModifiedOn] [datetime] NULL,
	[ModifiedBy] [int] NULL,
	[InterviewerAction] [bit] NULL,
 CONSTRAINT [PK_Trans_QCFormReport] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
Else

Begin
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Redflags')
      AND Object_ID = Object_ID(N'Trans_QCFormReport'))
BEGIN
    ALTER TABLE Trans_QCFormReport
    Add Redflags int
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ModifiedOn')
      AND Object_ID = Object_ID(N'Trans_QCFormReport'))
BEGIN
    ALTER TABLE Trans_QCFormReport
    Add ModifiedOn Datetime
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ModifiedBy')
      AND Object_ID = Object_ID(N'Trans_QCFormReport'))
BEGIN
    ALTER TABLE Trans_QCFormReport
    Add ModifiedBy int
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'InterviewerAction')
      AND Object_ID = Object_ID(N'Trans_QCFormReport'))
BEGIN
    ALTER TABLE Trans_QCFormReport
    Add InterviewerAction bit
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsReportBlocked')
      AND Object_ID = Object_ID(N'Trans_QCFormReport'))
BEGIN
    ALTER TABLE Trans_QCFormReport
    Add IsReportBlocked bit
END
END
GO


