IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetEmailLookupData'))
BEGIN
    DROP PROCEDURE dbo.usp_GetEmailLookupData
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetEmailLookupData]
    @EmailId NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    -- Result Set 1: Basic user info
    SELECT
        ul.id                               AS UserId,
        ul.FirstName,
        ul.LastName,
        ul.EmailId,
        CAST(ul.MobileNumber AS NVARCHAR(20)) AS MobileNumber,
        ISNULL(ul.IsActive, 0)              AS IsActive,
        ISNULL(p.Name, 'Unknown')           AS ProfileType
    FROM UserList ul
    LEFT JOIN Profile p ON p.id = ul.idProfile
    WHERE ul.EmailId = @EmailId;

    -- Result Set 2: Interviewer profile (empty if not an interviewer)
    SELECT
        ipr.Id,
        ISNULL(ipr.IsApproved, 0)           AS IsApproved,
        ISNULL(ipr.CurrentRole, '')          AS CurrentRole,
        ISNULL(ipr.Experience, 0)            AS Experience,
        ISNULL(ipr.City, '')                 AS City,
        ISNULL(ipr.Country, '')              AS Country,
        ISNULL(ipr.CompanyName, '')          AS CompanyName,
        ISNULL(ipr.IsLeadInterviewer, 0)     AS IsLeadInterviewer,
        ISNULL(ipr.NickName, '')             AS NickName,
        ISNULL(ipr.IsHoldPayment, 0)         AS IsHoldPayment,
        ISNULL(ipr.HoldPaymentReason, '')    AS HoldPaymentReason,
        ISNULL(ul.IsInterviewerAccess, 0)    AS IsInterviewerAccess,
        ISNULL(ipr.IsDeleted, 0)             AS IsDeleted,
        ISNULL(tic.PAN, '')                  AS PAN,
        ISNULL(tic.AccountName, '')          AS AccountName,
        ISNULL(tic.AccountNumber, '')        AS AccountNumber,
        ISNULL(tic.IFSCCode, '')             AS IFSCCode,
        ISNULL(tic.IsBankVerified, 0)        AS IsBankVerified,
        ISNULL(
            STUFF((
                SELECT DISTINCT ', ' + ski.Name
                FROM TransInterviewerSkill tis
                INNER JOIN Skill ski ON tis.SkillId = ski.id
                WHERE tis.InterviewerProfileId = ipr.Id
                FOR XML PATH('')
            ), 1, 2, ''),
        '') AS PrimarySkill
    FROM InterviewerProfile ipr
    INNER JOIN UserList ul ON ul.id = ipr.UserId
    LEFT JOIN TransInterviewerAccDetails tic ON tic.InterviewerProfileId = ipr.Id
    WHERE ul.EmailId = @EmailId;

    -- Result Set 3: Interview stats as Candidate
    -- confirmationStatus = 1 → active/scheduled, = 2 → cancelled
    SELECT
        COUNT(DISTINCT im.id)                                                                                            AS TotalCount,
        COUNT(DISTINCT CASE WHEN ic.MeetingStatus IN (4,6,7,8) AND im.confirmationStatus != 2 THEN im.id END)           AS TotalCompleted,
        COUNT(DISTINCT CASE WHEN im.confirmationStatus = 2   OR ic.MeetingStatus IN (1,3,5,9) THEN im.id END)           AS TotalCancelled,
        COUNT(DISTINCT CASE WHEN im.confirmationStatus != 2  AND (ic.MeetingStatus = 2 OR ic.MeetingStatus IS NULL)
                              THEN im.id END)                                                                            AS TotalScheduled
    FROM InterviewMeetings im
    LEFT JOIN InterviewComments ic ON ic.InterviewId = im.id
    WHERE im.candidateEmail = @EmailId
      AND ISNULL(im.IsDeleted, 0) = 0;

    -- Result Set 4: Interview stats as Interviewer
    SELECT
        COUNT(DISTINCT im.id)                                                                                            AS TotalCount,
        COUNT(DISTINCT CASE WHEN ic.MeetingStatus IN (4,6,7,8) AND im.confirmationStatus != 2 THEN im.id END)           AS TotalCompleted,
        COUNT(DISTINCT CASE WHEN im.confirmationStatus = 2   OR ic.MeetingStatus IN (1,3,5,9) THEN im.id END)           AS TotalCancelled,
        COUNT(DISTINCT CASE WHEN im.confirmationStatus != 2  AND (ic.MeetingStatus = 2 OR ic.MeetingStatus IS NULL)
                              THEN im.id END)                                                                            AS TotalScheduled
    FROM InterviewMeetings im
    LEFT JOIN InterviewComments ic ON ic.InterviewId = im.id
    WHERE im.interviewer = @EmailId
      AND ISNULL(im.IsDeleted, 0) = 0;

    -- Result Set 5: Payment stats from InterviewPoints (actual earned amounts)
    DECLARE @UserId INT;
    SELECT @UserId = id FROM UserList WHERE EmailId = @EmailId;

    SELECT
        ISNULL(SUM(
            CASE WHEN ip.Points >= 0
                 THEN ip.Points - (ip.Points * ISNULL(ip.PenaltyAmount, 0) / 100)
                 ELSE ip.Points END
        ), 0) AS TotalPaymentAmount,
        ISNULL(SUM(
            CASE WHEN ip.Status = 'Paid'
                 THEN ip.Points - (ip.Points * ISNULL(ip.PenaltyAmount, 0) / 100)
                 ELSE 0 END
        ), 0) AS TotalPaidAmount,
        ISNULL(SUM(
            CASE WHEN ip.Status = 'Pending' OR ip.Status = 'Hold'
                 THEN ip.Points - (ip.Points * ISNULL(ip.PenaltyAmount, 0) / 100)
                 ELSE 0 END
        ), 0) AS TotalPendingPayment
    FROM InterviewPoints ip
    WHERE ip.userid = @UserId
      AND ip.Points > 0;

    -- Result Set 6: Recent meetings as Candidate (last 20)
    SELECT TOP 20
        im.id                AS Id,
        im.IRFId,
        im.interviewDate     AS InterviewDate,
        im.interviewSlot     AS InterviewSlot,
        CASE
            WHEN im.confirmationStatus = 2    THEN 'Cancelled'
            WHEN ic.MeetingStatus IN (4,6,7,8) THEN 'Completed'
            WHEN ic.MeetingStatus IN (1,3,5,9) THEN 'No Show / Dropped'
            WHEN ic.MeetingStatus = 2          THEN 'Scheduled'
            ELSE 'Scheduled'
        END                  AS Status,
        im.candidateEmail    AS CandidateEmail,
        im.candidateName     AS CandidateName,
        im.interviewer       AS Interviewer
    FROM InterviewMeetings im
    LEFT JOIN InterviewComments ic ON ic.InterviewId = im.id
    WHERE im.candidateEmail = @EmailId
      AND ISNULL(im.IsDeleted, 0) = 0
    ORDER BY im.interviewDate DESC;

    -- Result Set 7: Recent meetings as Interviewer (last 20)
    SELECT TOP 20
        im.id                AS Id,
        im.IRFId,
        im.interviewDate     AS InterviewDate,
        im.interviewSlot     AS InterviewSlot,
        CASE
            WHEN im.confirmationStatus = 2     THEN 'Cancelled'
            WHEN ic.MeetingStatus IN (4,6,7,8) THEN 'Completed'
            WHEN ic.MeetingStatus IN (1,3,5,9) THEN 'No Show / Dropped'
            WHEN ic.MeetingStatus = 2          THEN 'Scheduled'
            ELSE 'Scheduled'
        END                  AS Status,
        im.candidateEmail    AS CandidateEmail,
        im.candidateName     AS CandidateName,
        im.interviewer       AS Interviewer
    FROM InterviewMeetings im
    LEFT JOIN InterviewComments ic ON ic.InterviewId = im.id
    WHERE im.interviewer = @EmailId
      AND ISNULL(im.IsDeleted, 0) = 0
    ORDER BY im.interviewDate DESC;

    -- Result Set 8: Recent payouts from PaymentHistory (last 20)
    SELECT TOP 20
        ph.id               AS Id,
        ph.PaymentDate,
        ISNULL(ph.Amount, 0)      AS Amount,
        ISNULL(ph.TotalAmount, 0) AS TotalAmount,
        ph.Status,
        ph.Comment,
        ph.PayoutStatus,
        ph.TransactionDate
    FROM PaymentHistory ph
    WHERE ph.Userid = @UserId
    ORDER BY ph.PaymentDate DESC;

END
GO
