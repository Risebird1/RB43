IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_ValidateProctoringCandidateInterview'))
BEGIN
    DROP PROCEDURE dbo.usp_ValidateProctoringCandidateInterview
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Backs the unauthenticated POST /api/proctoring/session/mint endpoint: a candidate has no
-- login, launch link, or API key, so (interviewId, candidateEmail) is validated as the
-- credential pair itself instead. "Joinable" mirrors
-- Risebird.Web ProctoringSessionEligibilityService.IsTerminalMeetingStatus: an interview is
-- joinable unless InterviewComments.MeetingStatus is one of the terminal values
-- (1 NoShow, 3 Dropped, 4 Completed, 5 NoShowInterviewer, 6 DroppedSelected, 7 DroppedRejected,
-- 8 DroppedBorderLine, 9 NoShowStatus) — NULL (no comments row yet) or 2 (Scheduled) both count
-- as joinable.
CREATE PROCEDURE [dbo].[usp_ValidateProctoringCandidateInterview]
    @InterviewId INT,
    @CandidateEmail NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ActualEmail VARCHAR(200) = NULL;
    DECLARE @MeetingStatus INT = NULL;

    SELECT TOP 1
        @ActualEmail = im.[candidateEmail],
        @MeetingStatus = ic.[MeetingStatus]
    FROM dbo.InterviewMeetings im
    LEFT JOIN dbo.InterviewComments ic ON ic.[InterviewId] = im.[id]
    WHERE im.[id] = @InterviewId
    ORDER BY ic.[id] DESC;

    SELECT
        CAST(CASE WHEN @ActualEmail IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS InterviewExists,
        CAST(CASE WHEN @ActualEmail IS NOT NULL
                       AND LOWER(LTRIM(RTRIM(@ActualEmail))) = LOWER(LTRIM(RTRIM(@CandidateEmail)))
                  THEN 1 ELSE 0 END AS BIT) AS CandidateEmailMatches,
        CAST(CASE WHEN @ActualEmail IS NULL THEN 0
                  WHEN ISNULL(@MeetingStatus, 2) IN (1, 3, 4, 5, 6, 7, 8, 9) THEN 0
                  ELSE 1 END AS BIT) AS IsJoinable
END
GO
