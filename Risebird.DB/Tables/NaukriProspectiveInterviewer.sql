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

    CONSTRAINT [PK_NaukriProspectiveInterviewer] PRIMARY KEY CLUSTERED ([Id] ASC)
)
END
GO
