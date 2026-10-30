IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_OpsToolkit_GetEmergencyUnassignments'))
BEGIN
    DROP PROCEDURE dbo.usp_OpsToolkit_GetEmergencyUnassignments
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Ops Toolkit: emergency interviews that were unassigned between @FromDate and @ToDate (inclusive, IST).
-- An unassign is an InterviewMeetingsHistory row with interviewer NULL whose previous history row had an
-- interviewer (usp_UnAssignInterviewer clears the column; tr_InterviewMeetings_ForUpdate logs the row).
-- Category:
--   PenaltyApplied - the unassigning interviewer has an unassign penalty row in InterviewPoints
--                    (written by usp_applyInterviewerpenalty when unassigned within 30 mins), or one that
--                    was removed from this page.
--   EINoPenalty    - the unassigning interviewer is an emergency interviewer of the IRF and no penalty exists.
CREATE PROCEDURE [dbo].[usp_OpsToolkit_GetEmergencyUnassignments]
    @FromDate DATE,
    @ToDate   DATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @From DATETIME = CAST(@FromDate AS DATETIME);
    DECLARE @To   DATETIME = DATEADD(DAY, 1, CAST(@ToDate AS DATETIME));

    ;WITH Unassign AS (
        SELECT h.id                                  AS InterviewId,
               h.PerformedAt                         AS UnassignedAt,
               LTRIM(RTRIM(prev.interviewer))        AS UnassignedBy
        FROM InterviewMeetingsHistory h
        CROSS APPLY (
            SELECT TOP 1 p.interviewer
            FROM InterviewMeetingsHistory p
            WHERE p.id = h.id AND p.PerformedAt < h.PerformedAt
            ORDER BY p.PerformedAt DESC
        ) prev
        WHERE h.PerformedAt >= @From
          AND h.PerformedAt <  @To
          AND h.IsEmergency = 1
          AND NULLIF(LTRIM(RTRIM(h.interviewer)), '') IS NULL
          AND NULLIF(LTRIM(RTRIM(prev.interviewer)), '') IS NOT NULL
    )
    SELECT
        CASE WHEN pen.id IS NOT NULL THEN 'PenaltyApplied' ELSE 'EINoPenalty' END AS Category,
        im.id                                       AS InterviewId,
        im.IRFId,
        im.candidateEmail                           AS CandidateEmail,
        im.candidateName                            AS CandidateName,
        im.interviewDate                            AS InterviewDate,
        im.confirmationStatus                       AS ConfirmationStatus,
        ISNULL(im.EIAssigned, 0)                    AS EIAssigned,
        im.interviewer                              AS CurrentInterviewer,
        u.UnassignedBy,
        ul.id                                       AS UnassignedByUserId,
        LTRIM(RTRIM(ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, ''))) AS UnassignedByName,
        u.UnassignedAt,
        DATEDIFF(MINUTE, u.UnassignedAt, im.interviewDate) AS MinutesBeforeInterview,
        CAST(CASE WHEN ei.IsEI = 1 THEN 1 ELSE 0 END AS BIT) AS IsEmergencyInterviewer,
        pen.id                                      AS InterviewPointsId,
        pen.Points                                  AS PenaltyPoints,
        pen.Status                                  AS PenaltyStatus,
        pen.Date                                    AS PenaltyDate,
        CAST(CASE WHEN pen.Points < 0 AND pen.Status = 'Pending' THEN 1 ELSE 0 END AS BIT) AS CanRemovePenalty,
        rem.OldPoints                               AS RemovedPoints,
        rem.PerformedBy                             AS RemovedBy,
        rem.PerformedAt                             AS RemovedAt,
        rem.Reason                                  AS RemovedReason
    FROM Unassign u
    JOIN InterviewMeetings im ON im.id = u.InterviewId
    LEFT JOIN UserList ul ON ul.EmailId = u.UnassignedBy
    LEFT JOIN IRF irf ON irf.IRFId = im.IRFId
    OUTER APPLY (
        SELECT TOP 1 1 AS IsEI
        FROM TransEmergencyInterviewer tei
        WHERE tei.IRFId = irf.id AND tei.UserId = ul.id
    ) ei
    OUTER APPLY (
        SELECT TOP 1 ip.id, ip.Points, ip.Status, ip.Date
        FROM InterviewPoints ip
        WHERE ip.Interviewid = im.id
          AND ip.userid = ul.id
          AND ip.Type = 'Interview'
          AND (ip.Points < 0
               OR ip.Comment LIKE 'Penalty %'
               OR EXISTS (SELECT 1 FROM OpsToolkitActionLog l
                          WHERE l.InterviewPointsId = ip.id AND l.ActionType = 'RemoveUnassignPenalty'))
        ORDER BY ip.id DESC
    ) pen
    OUTER APPLY (
        SELECT TOP 1 l.OldPoints, l.PerformedBy, l.PerformedAt, l.Reason
        FROM OpsToolkitActionLog l
        WHERE l.InterviewPointsId = pen.id AND l.ActionType = 'RemoveUnassignPenalty'
        ORDER BY l.Id DESC
    ) rem
    WHERE pen.id IS NOT NULL OR ei.IsEI = 1
    ORDER BY u.UnassignedAt DESC;
END
GO
