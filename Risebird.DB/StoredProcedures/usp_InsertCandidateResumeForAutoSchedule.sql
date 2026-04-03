-- usp_InsertCandidateResumeForAutoSchedule
-- Inserts a candidate resume with auto-scheduling metadata.
-- Used by the email-based auto-scheduling flow when candidate slot data
-- is extracted from emails with 100% confidence.
CREATE OR ALTER PROCEDURE [dbo].[usp_InsertCandidateResumeForAutoSchedule]
    @IRFId                      INT,
    @ResumeLink                 NVARCHAR(500),
    @AddedBy                    INT,
    @ClientHREmailId            VARCHAR(100) = NULL,
    @SupportTicketId            INT = NULL,
    @CandidateName              NVARCHAR(200) = NULL,
    @CandidateEmail             VARCHAR(150) = NULL,
    @ScheduledInterviewDateTime DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.CandidateResume (
        IRFId,
        ResumeLink,
        AddedBy,
        AddedOn,
        IsProcessed,
        IsResumeParsed,
        IsResumeParsingFailed,
        IsEmailMissing,
        ClientHREmailId,
        SupportTicketId,
        IsAutoScheduled,
        ScheduledInterviewDateTime,
        ExtractedCandidateName,
        ExtractedCandidateEmail
    )
    VALUES (
        @IRFId,
        @ResumeLink,
        @AddedBy,
        GETUTCDATE(),
        0,      -- IsProcessed = false (needs parsing)
        0,      -- IsResumeParsed = false
        0,      -- IsResumeParsingFailed = false
        0,      -- IsEmailMissing = false
        @ClientHREmailId,
        @SupportTicketId,
        1,      -- IsAutoScheduled = true
        @ScheduledInterviewDateTime,
        @CandidateName,
        @CandidateEmail
    );

    SELECT SCOPE_IDENTITY() AS NewId;
END
GO
