-- SupportTicketPendingResumes
-- Stores resume data awaiting HR review before ingestion into CandidateResume.
-- Maintains human-in-the-loop for all resume ingestion and scheduling.
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
        -- For scheduling (from email extraction)
        [HasSchedulingData]         BIT             NOT NULL DEFAULT 0,
        [ScheduledInterviewDateTime] DATETIME       NULL,
        [ScheduleTimezone]          VARCHAR(20)     NULL,
        -- Status tracking
        [Status]                    VARCHAR(20)     NOT NULL DEFAULT 'Pending',  -- Pending, Ingested, Rejected
        [IngestedResumeId]          INT             NULL,  -- FK to CandidateResume.id after ingestion
        [ProcessedAt]               DATETIME        NULL,
        [ProcessedBy]               VARCHAR(150)    NULL,
        [CreatedAt]                 DATETIME        NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_SupportTicketPendingResumes] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    
    CREATE NONCLUSTERED INDEX [IX_STPR_TicketId] ON [dbo].[SupportTicketPendingResumes] ([SupportTicketId]);
    CREATE NONCLUSTERED INDEX [IX_STPR_Status] ON [dbo].[SupportTicketPendingResumes] ([Status]);
END
GO

-- Add HasPendingResumes column to SupportTickets if not exists
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'HasPendingResumes' AND Object_ID = Object_ID(N'SupportTickets'))
BEGIN
    ALTER TABLE dbo.SupportTickets ADD [HasPendingResumes] BIT NOT NULL DEFAULT 0;
END
GO
