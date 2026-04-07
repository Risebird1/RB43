-- SupportTicketPendingResumes
-- Stores resume data awaiting HR review before ingestion into CandidateResume.
-- Maintains human-in-the-loop for all resume ingestion and scheduling.
-- Tracks IRF changes by HR for accuracy analysis.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'SupportTicketPendingResumes')
BEGIN
    CREATE TABLE [dbo].[SupportTicketPendingResumes] (
        [Id]                        INT             IDENTITY(1,1) NOT NULL,
        [SupportTicketId]           INT             NOT NULL,
        [ResumeUrl]                 NVARCHAR(1000)  NOT NULL,
        [CandidateName]             NVARCHAR(200)   NULL,
        [CandidateEmail]            VARCHAR(150)    NULL,
        -- Resolved IRF (may be null if couldn't resolve)
        [ResolvedIRFId]             INT             NULL,
        [ResolvedIRFStringId]       VARCHAR(20)     NULL,
        [ResolvedJobTitle]          NVARCHAR(200)   NULL,
        [ResolvedHREmail]           VARCHAR(150)    NULL,
        -- Original system-resolved IRF (for tracking accuracy)
        [OriginalResolvedIRFId]     INT             NULL,
        [OriginalResolvedIRFStringId] VARCHAR(20)   NULL,
        -- For scheduling (from email extraction)
        [HasSchedulingData]         BIT             NOT NULL DEFAULT 0,
        [ScheduledInterviewDateTime] DATETIME       NULL,
        [ScheduleTimezone]          VARCHAR(20)     NULL,
        -- Status tracking
        [Status]                    VARCHAR(20)     NOT NULL DEFAULT 'Pending',  -- Pending, Ingested, Rejected
        [IngestedResumeId]          INT             NULL,  -- FK to CandidateResume.id after ingestion
        [ProcessedAt]               DATETIME        NULL,
        [ProcessedBy]               VARCHAR(150)    NULL,
        -- IRF change tracking
        [IRFChangedByHR]            BIT             NOT NULL DEFAULT 0,
        [IRFChangedAt]              DATETIME        NULL,
        [IRFChangeReason]           NVARCHAR(500)   NULL,
        [CreatedAt]                 DATETIME        NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_SupportTicketPendingResumes] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    
    CREATE NONCLUSTERED INDEX [IX_STPR_TicketId] ON [dbo].[SupportTicketPendingResumes] ([SupportTicketId]);
    CREATE NONCLUSTERED INDEX [IX_STPR_Status] ON [dbo].[SupportTicketPendingResumes] ([Status]);
    CREATE NONCLUSTERED INDEX [IX_STPR_IRFChanged] ON [dbo].[SupportTicketPendingResumes] ([IRFChangedByHR]) WHERE [IRFChangedByHR] = 1;
END
GO

-- Add new columns if table already exists
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'OriginalResolvedIRFId' AND Object_ID = Object_ID(N'SupportTicketPendingResumes'))
BEGIN
    ALTER TABLE dbo.SupportTicketPendingResumes ADD [OriginalResolvedIRFId] INT NULL;
    ALTER TABLE dbo.SupportTicketPendingResumes ADD [OriginalResolvedIRFStringId] VARCHAR(20) NULL;
    ALTER TABLE dbo.SupportTicketPendingResumes ADD [IRFChangedByHR] BIT NOT NULL DEFAULT 0;
    ALTER TABLE dbo.SupportTicketPendingResumes ADD [IRFChangedAt] DATETIME NULL;
    ALTER TABLE dbo.SupportTicketPendingResumes ADD [IRFChangeReason] NVARCHAR(500) NULL;
END
GO

-- Add HasPendingResumes column to SupportTickets if not exists
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'HasPendingResumes' AND Object_ID = Object_ID(N'SupportTickets'))
BEGIN
    ALTER TABLE dbo.SupportTickets ADD [HasPendingResumes] BIT NOT NULL DEFAULT 0;
END
GO

-- Add ResumeFileName column if not exists (stores original attachment filename)
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'ResumeFileName' AND Object_ID = Object_ID(N'SupportTicketPendingResumes'))
BEGIN
    ALTER TABLE dbo.SupportTicketPendingResumes ADD [ResumeFileName] NVARCHAR(500) NULL;
END
GO
