-- Migration: Priority 1 performance — hot-path indexes + Sendgrid timestamp fix
-- Deploy alongside optimized SPs (2026-06-17)

-- ---------------------------------------------------------------------------
-- SendgridEventData: numeric sort key for varchar Unix timestamp
-- ---------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.SendgridEventData')
      AND name = N'EventTimestampUnix'
)
BEGIN
    ALTER TABLE dbo.SendgridEventData
    ADD EventTimestampUnix AS TRY_CAST([timestamp] AS BIGINT) PERSISTED;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_SendgridEventData_Email_EventTimestamp'
      AND object_id = OBJECT_ID(N'dbo.SendgridEventData')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_SendgridEventData_Email_EventTimestamp
    ON dbo.SendgridEventData (email, EventTimestampUnix DESC)
    INCLUDE ([event], sg_message_id, Split_Message_Id, CreatedAt);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_SendgridEventData_Email_BounceDrop'
      AND object_id = OBJECT_ID(N'dbo.SendgridEventData')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_SendgridEventData_Email_BounceDrop
    ON dbo.SendgridEventData (email)
    WHERE [event] LIKE '%bounce%' OR [event] LIKE '%drop%';
END
GO

-- ---------------------------------------------------------------------------
-- InterviewMeetings: date-range dashboard queries (replaces CAST(interviewDate AS DATE))
-- ---------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_InterviewMeetings_InterviewDate_Confirmation'
      AND object_id = OBJECT_ID(N'dbo.InterviewMeetings')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_InterviewMeetings_InterviewDate_Confirmation
    ON dbo.InterviewMeetings (interviewDate, confirmationStatus)
    INCLUDE (IRFId, interviewSlot, interviewer, candidateEmail, IsDeleted, RedoInterview, id);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_InterviewMeetings_IRFId_InterviewDate'
      AND object_id = OBJECT_ID(N'dbo.InterviewMeetings')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_InterviewMeetings_IRFId_InterviewDate
    ON dbo.InterviewMeetings (IRFId, interviewDate)
    INCLUDE (confirmationStatus, candidateEmail, interviewer, interviewSlot, IsDeleted);
END
GO

-- ---------------------------------------------------------------------------
-- ZoomMeetingTransmission: per-meeting lookups (ribbon / payment SLA)
-- ---------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ZoomMeetingTransmission_MeetingId_Event'
      AND object_id = OBJECT_ID(N'dbo.ZoomMeetingTransmission')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ZoomMeetingTransmission_MeetingId_Event
    ON dbo.ZoomMeetingTransmission (MeetingId, Event)
    INCLUDE (ParticipantName, id);
END
GO

-- ---------------------------------------------------------------------------
-- Candidate / status lookups
-- ---------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Candidate_IRFId'
      AND object_id = OBJECT_ID(N'dbo.Candidate')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Candidate_IRFId
    ON dbo.Candidate (IRFId)
    INCLUDE (CandidateMasterId, Phone, Status, PlivoCXDisposition);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_CandidatesMaster_UserId'
      AND object_id = OBJECT_ID(N'dbo.CandidatesMaster')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_CandidatesMaster_UserId
    ON dbo.CandidatesMaster (UserId);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_CandidateStatus_IRFId_Status_Date'
      AND object_id = OBJECT_ID(N'dbo.CandidateStatus')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_CandidateStatus_IRFId_Status_Date
    ON dbo.CandidateStatus (IRFId, [Status], [Date])
    INCLUDE (CandidateId);
END
GO

-- ---------------------------------------------------------------------------
-- Communication counts for IRF candidate lists
-- ---------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_SentMailDetails_IRFId'
      AND object_id = OBJECT_ID(N'dbo.SentMailDetails')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_SentMailDetails_IRFId
    ON dbo.SentMailDetails (IRFId)
    INCLUDE ([To]);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_SentWhatsAppMessageDetails_IRFId_Recipient'
      AND object_id = OBJECT_ID(N'dbo.SentWhatsAppMessageDetails')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_SentWhatsAppMessageDetails_IRFId_Recipient
    ON dbo.SentWhatsAppMessageDetails (IRFId, Recipient);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_CallRecordingData_IRFId_Recipient'
      AND object_id = OBJECT_ID(N'dbo.CallRecordingData')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_CallRecordingData_IRFId_Recipient
    ON dbo.CallRecordingData (IRFId, CallRecipient);
END
GO

-- ---------------------------------------------------------------------------
-- Plivo reschedule SP (complements Optimize_GetEligibleCandidatesForPlivoCXCalls.sql)
-- ---------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_PlivoCXCallLogs_Reschedule'
      AND object_id = OBJECT_ID(N'dbo.PlivoCXCallLogs')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_PlivoCXCallLogs_Reschedule
    ON dbo.PlivoCXCallLogs (RecipientEmail, IRFId, CallPurpose, CreatedAt)
    INCLUDE (CallStatus, BookingApiSuccess, BookedInterviewDateTime)
    WHERE CallPurpose = 'reschedule';
END
GO
