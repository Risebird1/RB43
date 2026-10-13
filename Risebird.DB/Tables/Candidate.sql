/****** Object:  Table [dbo].[Candidate]    Script Date: 25-09-2019 7.49.24 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'Candidate')
BEGIN
CREATE TABLE [dbo].[Candidate](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	[ResumeLink] [varchar](500) NULL,
	[TotalInvites] [int] NULL,
	[Type] [varchar](100) NULL,
	[Location] [varchar](100) NULL,
	[Skill] [varchar](500) NULL,
	[WorkAuth] [varchar](100) NULL,
	[CurrentLocation] [varchar](100) NULL,
	[CurrentEmployer] [varchar](200) NULL,
	[Phone] [nvarchar](200) NULL,
	[TotalExperience] [decimal](10, 2) NULL,
	[CurrentSalary] [decimal](10, 2) NULL,
	[ExpectedSalary] [decimal](10, 2) NULL,
	[Notice] [decimal](10, 2) NULL,
	[SelectabilityComments] [varchar](max) NULL,
	[JoinabilityComments] [varchar](max) NULL,
	[JoinabilityScore] [decimal](10, 2) NULL,
	[SelectabilityScore] [decimal](10, 2) NULL,
	[TechSkillComments] [varchar](max) NULL,
	[SkillGeneralComments] [varchar](max) NULL,
	[TechSkillScore] [decimal](10, 2) NULL,
	[SkillGeneralScore] [decimal](10, 2) NULL,
	[AddedByUserId] [int] NULL,
	[VoiceItUserId] [nvarchar](max) NULL,
	[InviteDate] [datetime] NULL,
	[Status] [int] NULL,
	[CandidateMasterId] [int] NULL,
	[CostEstimate] [nvarchar](200) NULL,
	[EncodedResumeLink] [varchar](500) NULL,
	[Heuristicsscore] [int] NULL,
	[AvailableFullTime] [bit] NULL,
	[AvailableFreeLancing] [bit] NULL,
	[ClientHRAttemptContact] [varchar](100) NULL,
	[ProfileSource] [varchar](200) NULL,
	[FitmentStatus] [int] NULL,
	[PriorityStatus] [int] NULL,
	[CompletionStatus] [int] NULL,
	[IsProctor] [bit] NULL,
	[AffindaResumeIdentifier] [varchar](75) NULL,
	[CodedResumeLink] [varchar](2000) NULL,
	[IsResumeRedactAttempted] [bit] NULL,
	[IsResumeRedactFailed] [bit] NULL,
	[PreReportLink] [varchar](500) NULL,
	[PreReportUsername] [varchar](50) NULL,
	[PreReportPassword] [varchar](50) NULL,
	[OpenAIScreeningResponseJson] [varchar](max) NULL,
	[ResumeSummary] [varchar](max) NULL,
	[AIResumeScreeningVerdict] [varchar](30) NULL,
	[AIScreeningResultJsonV2] [varchar](max) NULL,
	[AIResumeScreeningScore] [int] NULL,
	[VapiCallAttempts] [int] NOT NULL DEFAULT 0,
 CONSTRAINT [PK_Candidate] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

ALTER TABLE [dbo].[Candidate] ADD  DEFAULT ((0)) FOR [TotalInvites]


ALTER TABLE [dbo].[Candidate]  WITH CHECK ADD FOREIGN KEY([CandidateMasterId])
REFERENCES [dbo].[CandidatesMaster] ([id])

ALTER TABLE [dbo].[Candidate]  WITH CHECK ADD FOREIGN KEY([IRFId])
REFERENCES [dbo].[IRF] ([id])
End
ELSE
BEGIN
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Status')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add [Status] INT
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'TotalInvites')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add TotalInvites INT DEFAULT 0
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Type')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add [Type] VARCHAR(100)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Location')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add [Location] VARCHAR(100)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Skill')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add Skill VARCHAR(500)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'WorkAuth')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add WorkAuth VARCHAR(100)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CurrentLocation')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add CurrentLocation VARCHAR(100)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CurrentEmployer')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add CurrentEmployer VARCHAR(200)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'SelectabilityComments')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add SelectabilityComments VARCHAR(max)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'JoinabilityComments')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add JoinabilityComments VARCHAR(max)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'JoinabilityComments')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add JoinabilityComments VARCHAR(max)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'TechSkillComments')
      AND Object_ID = OBJECT_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    ADD TechSkillComments VARCHAR(MAX)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'SkillGeneralComments')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add SkillGeneralComments VARCHAR(max)
END


IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Phone')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add Phone NVARCHAR(200)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'TotalExperience')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add TotalExperience decimal (10,2)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CurrentSalary')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add CurrentSalary decimal (10,2)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ExpectedSalary')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add ExpectedSalary decimal (10,2)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Notice')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add Notice decimal (10,2)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'JoinabilityScore')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add JoinabilityScore decimal (10,2)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'SelectabilityScore')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add SelectabilityScore decimal (10,2)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'TechSkillScore')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add TechSkillScore decimal (10,2)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'SkillGeneralScore')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add SkillGeneralScore decimal (10,2)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AddedByUserId')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add [AddedByUserId] int
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'VoiceItUserId')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add VoiceItUserId NVARCHAR(max)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'InviteDate')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add InviteDate [datetime] NULL
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CostEstimate')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add [CostEstimate] [nvarchar](200) NULL
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'EncodedResumeLink')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add  [EncodedResumeLink] [varchar](500) NULL
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Heuristicsscore')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add  [Heuristicsscore] int NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AvailableFullTime')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add AvailableFullTime Bit
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AvailableFreeLancing')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add AvailableFreeLancing Bit
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ClientHRAttemptContact')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add [ClientHRAttemptContact] VARCHAR(100)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ProfileSource')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add [ProfileSource] VARCHAR(200)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'FitmentStatus')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add FitmentStatus INT
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PriorityStatus')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add PriorityStatus INT
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CompletionStatus')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add CompletionStatus INT
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsProctor')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add IsProctor Bit
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AffindaResumeIdentifier')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add [AffindaResumeIdentifier] VARCHAR(75)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CodedResumeLink')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add [CodedResumeLink] VARCHAR(2000)
END

IF Not EXISTS(SELECT * FROM sys.columns WHERE Name IN (N'IsResumeRedactAttempted')
AND Object_ID = Object_ID(N'Candidate'))
BEGIN
	alter table Candidate
	Add IsResumeRedactAttempted bit null
END

IF Not EXISTS(SELECT * FROM sys.columns WHERE Name IN (N'IsResumeRedactFailed')
AND Object_ID = Object_ID(N'Candidate'))
BEGIN
	alter table Candidate
	Add IsResumeRedactFailed bit null
END
IF Not EXISTS(SELECT * FROM sys.columns WHERE Name IN (N'PreReportLink')
AND Object_ID = Object_ID(N'Candidate'))
BEGIN
	alter table Candidate
	Add PreReportLink varchar(500) null
END
IF Not EXISTS(SELECT * FROM sys.columns WHERE Name IN (N'PreReportUsername')
AND Object_ID = Object_ID(N'Candidate'))
BEGIN
	alter table Candidate
	Add PreReportUsername varchar(50) null
END
IF Not EXISTS(SELECT * FROM sys.columns WHERE Name IN (N'PreReportPassword')
AND Object_ID = Object_ID(N'Candidate'))
BEGIN
	alter table Candidate
	Add PreReportPassword varchar(50) null
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'OpenAIScreeningResponseJson')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add OpenAIScreeningResponseJson VARCHAR(max)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ResumeSummary')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add ResumeSummary VARCHAR(max) -- redacted resume summary, using ai, that hides out all personal details 
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AIResumeScreeningVerdict')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add AIResumeScreeningVerdict VARCHAR(30)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AIScreeningResultJsonV2')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add AIScreeningResultJsonV2 VARCHAR(max) 
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AIResumeScreeningScore')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add AIResumeScreeningScore int
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'VapiCallAttempts')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    ALTER TABLE Candidate
    Add VapiCallAttempts INT NOT NULL DEFAULT 0
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PlivoCXDisposition')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    -- Disposition captured by the Plivo CX AI agent during outbound scheduling calls.
    -- Values: 'interested' | 'uninterested' | 'cant_say'
    -- Updated automatically each time a Plivo CX call completes for this candidate.
    ALTER TABLE Candidate
    ADD PlivoCXDisposition VARCHAR(20) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PlivoWhatsAppAttempts')
      AND Object_ID = Object_ID(N'Candidate'))
BEGIN
    -- Tracks the number of outbound Plivo WhatsApp messages (schedule + reschedule)
    -- initiated for this candidate. Incremented by usp_IncrementCandidatePlivoWhatsAppAttempts
    -- immediately after each successful trigger. Kept separate from VapiCallAttempts
    -- (voice) so the two channels can be monitored and capped independently.
    ALTER TABLE Candidate
    ADD PlivoWhatsAppAttempts INT NOT NULL DEFAULT 0
END

End

GO



