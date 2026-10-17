-- =============================================
-- PlivoCX campaigns: the "By IRF" candidate picker on Create campaign.
-- Every candidate of the IRF with the facts the picker labels them by; whether a candidate is
-- callable for the chosen agent comes from the eligibility procedures
-- (usp_GetEligibleCandidatesForPlivoCXCalls / ...Reschedule with @IRFId), merged in code.
--
--   @IRFId  : IRF.id
--   @NowIST : current IST time (InterviewMeetings.interviewDate is IST)
--
-- HasMeeting           : a future meeting that is not cancelled (confirmationStatus <> '2')
-- UpcomingInterviewId / UpcomingInterviewDate : the next such meeting (for confirm agents), with its time
-- LastInterviewDate    : the latest past meeting (context for reschedule agents)
-- IsCold               : Candidate.Status = 9
-- ActiveCampaignName   : a Draft / Running / Paused campaign the candidate is Pending in
-- =============================================
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetPlivoCXCampaignPickerForIRF'))
BEGIN
    DROP PROCEDURE dbo.usp_GetPlivoCXCampaignPickerForIRF
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetPlivoCXCampaignPickerForIRF]
    @IRFId  INT,
    @NowIST DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @NowIST = ISNULL(@NowIST, GETDATE());

    SELECT
        c.id                AS CandidateId,
        c.IRFId,
        irf.IRFId           AS IRFName,
        u.FirstName         AS CandidateName,
        u.EmailId           AS CandidateEmail,
        c.Phone             AS CandidatePhone,
        irf.JDTiltle        AS JobTitle,
        cl.Name             AS ClientName,
        c.PlivoCXDisposition,
        CAST(CASE WHEN c.Status = 9 THEN 1 ELSE 0 END AS BIT) AS IsCold,
        CAST(CASE WHEN nextMeeting.id IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS HasMeeting,
        nextMeeting.id      AS UpcomingInterviewId,
        FORMAT(nextMeeting.interviewDate, 'dd MMM yyyy, h:mm tt', 'en-US') AS UpcomingInterviewDate,
        CONVERT(NVARCHAR(50), lastMeeting.interviewDate, 106) AS LastInterviewDate,
        activeCampaign.Name AS ActiveCampaignName
    FROM       dbo.Candidate        c
    INNER JOIN dbo.IRF              irf ON irf.id = c.IRFId
    INNER JOIN dbo.Client           cl  ON cl.id  = irf.ClientId
    INNER JOIN dbo.CandidatesMaster cm  ON cm.id  = c.CandidateMasterId
    INNER JOIN dbo.UserList         u   ON u.id   = cm.UserId
    OUTER APPLY (
        SELECT TOP (1) im.id, im.interviewDate
        FROM dbo.InterviewMeetings im
        WHERE im.candidateEmail = u.EmailId
          AND im.IRFId = irf.IRFId
          AND ISNULL(im.IsDeleted, 0) = 0
          AND ISNULL(im.confirmationStatus, '') <> '2'
          AND im.interviewDate > @NowIST
        ORDER BY im.interviewDate ASC
    ) nextMeeting
    OUTER APPLY (
        SELECT TOP (1) im.interviewDate
        FROM dbo.InterviewMeetings im
        WHERE im.candidateEmail = u.EmailId
          AND im.IRFId = irf.IRFId
          AND im.interviewDate <= @NowIST
        ORDER BY im.interviewDate DESC
    ) lastMeeting
    OUTER APPLY (
        SELECT TOP (1) pc.Name
        FROM dbo.PlivoCXCampaignCandidate pcc
        INNER JOIN dbo.PlivoCXCampaign pc ON pc.Id = pcc.CampaignId
        WHERE pcc.CandidateId = c.id
          AND pcc.[State] = 'Pending'
          AND pc.[Status] IN ('Draft', 'Running', 'Paused')
        ORDER BY pc.Id DESC
    ) activeCampaign
    WHERE c.IRFId = @IRFId
      AND u.EmailId IS NOT NULL
    ORDER BY c.id DESC;
END
GO
