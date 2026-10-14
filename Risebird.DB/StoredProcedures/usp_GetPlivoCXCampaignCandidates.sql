-- =============================================
-- PlivoCX campaigns: the candidate table on the campaign detail page.
--   @CampaignId : the campaign
--
-- Result 1 : every candidate row, with the latest call's recording and status.
-- Result 2 : call statistics for the tiles - total calls that finished (anything that is not
--            still initiated / triggered) and how many of them were answered (completed).
-- =============================================
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetPlivoCXCampaignCandidates'))
BEGIN
    DROP PROCEDURE dbo.usp_GetPlivoCXCampaignCandidates
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetPlivoCXCampaignCandidates]
    @CampaignId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        cc.Id,
        cc.CampaignId,
        cc.CandidateId,
        cc.CandidateName,
        cc.CandidateEmail,
        cc.CandidatePhone,
        cc.IRFId,
        cc.IRFName,
        cc.JobTitle,
        cc.ClientName,
        cc.InterviewId,
        cc.OriginalInterviewDate,
        cc.[State],
        cc.Attempts,
        cc.LastCallAt,
        cc.NextAttemptAt,
        cc.LastDisposition,
        cc.BookedSlot,
        cc.LastCallLogId,
        cc.CreatedAt,
        cc.UpdatedAt,
        l.RecordingUrl  AS LastRecordingUrl,
        l.CallStatus    AS LastCallStatus
    FROM      dbo.PlivoCXCampaignCandidate cc
    LEFT JOIN dbo.PlivoCXCallLogs          l ON l.Id = cc.LastCallLogId
    WHERE cc.CampaignId = @CampaignId
    ORDER BY
        CASE cc.[State] WHEN 'Pending' THEN 0 WHEN 'Booked' THEN 1 WHEN 'NotBooked' THEN 2 ELSE 3 END,
        cc.LastCallAt DESC,
        cc.Id;

    SELECT
        SUM(CASE WHEN l.CallStatus NOT IN ('initiated', 'triggered') THEN 1 ELSE 0 END) AS FinishedCalls,
        SUM(CASE WHEN l.CallStatus = 'completed' THEN 1 ELSE 0 END)                      AS AnsweredCalls,
        SUM(CASE WHEN l.CallStatus IN ('initiated', 'triggered')
                  AND l.CreatedAt >= DATEADD(MINUTE, -30, GETUTCDATE()) THEN 1 ELSE 0 END) AS InFlightCalls
    FROM dbo.PlivoCXCallLogs l
    WHERE l.CampaignId = @CampaignId;
END
GO
