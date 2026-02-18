-- Table to store Vapi call logs and interview booking details
CREATE TABLE [dbo].[VapiCallLogs]
(
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [VapiCallId] NVARCHAR(100) NOT NULL UNIQUE,
    [InterviewId] INT NULL,
    [RecipientId] INT NULL,
    [RecipientPhone] NVARCHAR(20) NOT NULL,
    [RecipientName] NVARCHAR(255) NULL,
    [RecipientEmail] NVARCHAR(255) NULL,
    [CallStatus] NVARCHAR(50) NULL, -- queued, ringing, in-progress, forwarding, ended
    [ScheduledDateTime] DATETIME NULL,
    [CallStartedAt] DATETIME NULL,
    [CallEndedAt] DATETIME NULL,
    [EndedReason] NVARCHAR(100) NULL, -- user-ended, assistant-ended, voicemail, no-answer, etc.
    [DurationSeconds] INT NULL,
    [Transcript] NVARCHAR(MAX) NULL,
    [RecordingUrl] NVARCHAR(500) NULL,
    [InterviewBooked] BIT NULL,
    [BookedInterviewDateTime] DATETIME NULL,
    [BookingConfirmationDetails] NVARCHAR(MAX) NULL,
    [CallCost] DECIMAL(10, 4) NULL,
    [AssistantId] NVARCHAR(100) NULL,
    [Summary] NVARCHAR(MAX) NULL,
    [StructuredData] NVARCHAR(MAX) NULL, -- JSON data extracted from call
    [CreatedAt] DATETIME NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME NULL,
    [CreatedBy] NVARCHAR(255) NULL,
    [UpdatedBy] NVARCHAR(255) NULL,
    
    INDEX [IX_VapiCallLogs_VapiCallId] NONCLUSTERED ([VapiCallId]),
    INDEX [IX_VapiCallLogs_InterviewId] NONCLUSTERED ([InterviewId]),
    INDEX [IX_VapiCallLogs_RecipientId] NONCLUSTERED ([RecipientId]),
    INDEX [IX_VapiCallLogs_CreatedAt] NONCLUSTERED ([CreatedAt] DESC)
);
GO

-- Add description
EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Stores logs of Vapi AI voice calls for interview booking', 
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'VapiCallLogs';
GO
