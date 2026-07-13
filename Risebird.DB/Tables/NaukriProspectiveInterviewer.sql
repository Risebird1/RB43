IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'NaukriProspectiveInterviewer')
BEGIN
CREATE TABLE [dbo].[NaukriProspectiveInterviewer](
    [Id]                        [int] IDENTITY(1,1) NOT NULL,
    [SenderEmail]               [varchar](150) NOT NULL,
    [Subject]                   [nvarchar](500) NULL,
    [MailContent]               [nvarchar](MAX) NULL,
    [ReceivedAt]                [datetime] NULL,

    -- Fields extracted from Naukri email body
    [CandidateName]             [nvarchar](200) NULL,
    [Location]                  [nvarchar](200) NULL,
    [PastExperience]            [nvarchar](500) NULL,
    [NoticePeriod]              [nvarchar](100) NULL,
    [Education]                 [nvarchar](500) NULL,
    [KeySkills]                 [nvarchar](MAX) NULL,
    [CurrentRole]               [nvarchar](200) NULL,
    [ExperienceYears]           [nvarchar](50) NULL,
    [Salary]                    [nvarchar](50) NULL,
    [MagName]                   [nvarchar](200) NULL,

    -- Resume
    [ResumeLink]                [nvarchar](2000) NULL,
    [HasAttachment]             [bit] NOT NULL DEFAULT 0,

    -- Parsed from resume (Step 1)
    [ParsedEmail]               [varchar](150) NULL,
    [ParsedPhone]               [varchar](50) NULL,
    [ResumeParseCompleted]      [bit] NULL,
    [IsExistingInterviewer]     [bit] NULL,
    [ExistingUserId]            [int] NULL,
    [ExistingProfileId]         [int] NULL,

    -- Employment Verification (Step 2)
    [EmploymentVerified]        [bit] NULL,
    [EmploymentCompanyName]     [nvarchar](300) NULL,
    [EmploymentIsEmployed]      [bit] NULL,
    [EmploymentDetails]         [nvarchar](MAX) NULL,

    -- AI Resume Analysis (Step 3)
    [ResumeAIAnalysisCompleted] [bit] NULL,
    [ResumeAIAnalysisResult]    [nvarchar](MAX) NULL,
    [AIVerdict]                 [varchar](50) NULL,
    [AIFitmentScore]            [int] NULL,

    -- Onboarding (Step 4)
    [OnboardingCompleted]       [bit] NULL,
    [OnboardedUserId]           [int] NULL,
    [OnboardedProfileId]        [int] NULL,
    [AssessmentGroupId]         [int] NULL,
    [AcknowledgementEmailSent]  [bit] NULL,

    -- Pipeline state
    [Status]                    [varchar](50) NOT NULL DEFAULT 'Open',
    [ProcessedBy]               [varchar](150) NULL,
    [CreatedAt]                 [datetime] NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]                 [datetime] NULL,

    -- Audit log for manual overrides (JSON array)
    [ModificationLog]           [nvarchar](MAX) NULL,

    CONSTRAINT [PK_NaukriProspectiveInterviewer] PRIMARY KEY CLUSTERED ([Id] ASC)
)
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'ModificationLog' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [ModificationLog] NVARCHAR(MAX) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'Source' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [Source] VARCHAR(50) NOT NULL DEFAULT 'Naukri'
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'SelectedIRFId' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [SelectedIRFId] INT NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'SelectedIRFName' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [SelectedIRFName] NVARCHAR(300) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'ApplicationLink' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [ApplicationLink] NVARCHAR(2000) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'InvitationEmailSent' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [InvitationEmailSent] BIT NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'InvitationEmailSentAt' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [InvitationEmailSentAt] DATETIME NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'ProspectResponse' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [ProspectResponse] VARCHAR(50) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'ProspectResponseAt' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [ProspectResponseAt] DATETIME NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'SelfRegistrationCompleted' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [SelfRegistrationCompleted] BIT NULL
END
GO

-- Prospect Onboarding redesign (Phase 1): unified intent + ticket/funnel split + onboarding type
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'Intent' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [Intent] VARCHAR(50) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'IntentSource' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [IntentSource] VARCHAR(50) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'IntentConfirmedAt' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [IntentConfirmedAt] DATETIME NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'TicketStatus' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [TicketStatus] VARCHAR(50) NULL DEFAULT 'Open'
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'FunnelStage' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [FunnelStage] VARCHAR(50) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'OnboardingType' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [OnboardingType] VARCHAR(50) NULL
END
GO

-- Ticket assignment: the operator (InterviewerManagement / SeniorInterviewerManagement) who owns
-- this prospect and is accountable for its closure (mirrors SupportTicket assignment).
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'AssignedToUserId' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [AssignedToUserId] INT NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'AssignedToEmail' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [AssignedToEmail] VARCHAR(150) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'AssignedToName' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [AssignedToName] NVARCHAR(200) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'AssignedAt' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [AssignedAt] DATETIME NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.indexes WHERE name = N'IX_NaukriProspectiveInterviewer_AssignedToUserId' AND object_id = OBJECT_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_NaukriProspectiveInterviewer_AssignedToUserId]
        ON [dbo].[NaukriProspectiveInterviewer] ([AssignedToUserId])
END
GO

-- LinkedIn background-verification verdict (Relay "LinkedIn Verified" variant).
-- Captured at webhook ingestion; only verified prospects reach the webhook, so these
-- record WHICH profile matched and HOW confident the check was, for audit purposes.
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'LinkedInUrl' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [LinkedInUrl] NVARCHAR(2000) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'LinkedInMatch' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [LinkedInMatch] VARCHAR(10) NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'LinkedInConfidence' AND Object_ID = Object_ID(N'NaukriProspectiveInterviewer'))
BEGIN
    ALTER TABLE NaukriProspectiveInterviewer ADD [LinkedInConfidence] INT NULL
END
GO
