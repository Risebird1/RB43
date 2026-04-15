IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'InterviewerEmailAvailability')
BEGIN
CREATE TABLE [dbo].[InterviewerEmailAvailability](
    [Id]                        [int] IDENTITY(1,1) NOT NULL,
    [SenderEmail]               [varchar](150) NOT NULL,
    [UserId]                    [int] NULL,
    [InterviewerProfileId]      [int] NULL,
    [Subject]                   [nvarchar](500) NULL,
    [MailContent]               [nvarchar](MAX) NULL,
    [ReceivedAt]                [datetime] NULL,
    [JobTitle]                  [nvarchar](200) NULL,
    [InterviewerName]           [nvarchar](200) NULL,
    [Phone]                     [varchar](50) NULL,
    [MagName]                   [nvarchar](200) NULL,
    [IRFReference]              [nvarchar](100) NULL,
    [HasLoginCredentials]       [varchar](10) NULL,
    [SlotsPerDay]               [varchar](20) NULL,
    [TimeFrame]                 [nvarchar](500) NULL,
    [AvailabilityIntent]        [nvarchar](MAX) NULL,
    [MailBodyIntent]            [nvarchar](500) NULL,
    [ConcernsOrNotInterested]   [nvarchar](MAX) NULL,
    [ResumeFitment]             [varchar](50) NULL,
    [ResumeLink]                [nvarchar](2000) NULL,
    [HasAttachment]             [bit] NOT NULL DEFAULT 0,
    [Status]                    [varchar](50) NOT NULL DEFAULT 'Open',
    [EmploymentVerified]        [bit] NULL,
    [ResumeAIAnalysisCompleted] [bit] NULL,
    [ResumeAIAnalysisResult]    [nvarchar](MAX) NULL,
    [ProcessedBy]               [varchar](150) NULL,
    [CreatedAt]                 [datetime] NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]                 [datetime] NULL,
    CONSTRAINT [PK_InterviewerEmailAvailability] PRIMARY KEY CLUSTERED ([Id] ASC)
)
END
ELSE
BEGIN
    PRINT 'Table InterviewerEmailAvailability already exists.'
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'AssessmentGroupId' AND Object_ID = Object_ID(N'InterviewerEmailAvailability'))
BEGIN
    ALTER TABLE InterviewerEmailAvailability ADD [AssessmentGroupId] INT NULL
END
GO

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'AssessmentGroupAssignedAt' AND Object_ID = Object_ID(N'InterviewerEmailAvailability'))
BEGIN
    ALTER TABLE InterviewerEmailAvailability ADD [AssessmentGroupAssignedAt] DATETIME NULL
END
GO
