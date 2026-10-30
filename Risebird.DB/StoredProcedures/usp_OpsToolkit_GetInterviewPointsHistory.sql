IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_OpsToolkit_GetInterviewPointsHistory'))
BEGIN
    DROP PROCEDURE dbo.usp_OpsToolkit_GetInterviewPointsHistory
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Ops Toolkit history popup for one interview.
-- Result set 1: every InterviewPoints entry for the interview (all users and types).
-- Result set 2: interviewer assignment changes from InterviewMeetingsHistory.
-- Result set 3: Ops Toolkit actions taken on the interview.
CREATE PROCEDURE [dbo].[usp_OpsToolkit_GetInterviewPointsHistory]
    @InterviewId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ip.id                                   AS InterviewPointsId,
        ip.userid                               AS UserId,
        ul.EmailId,
        LTRIM(RTRIM(ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, ''))) AS Name,
        ip.Points,
        ip.Type,
        ip.Comment,
        ip.Status,
        ip.Date,
        ip.PenaltyAmount,
        ip.QCPoints,
        ip.PaymentHistoryId,
        CAST(CASE WHEN ip.Type = 'Interview' AND ip.Points < 0 AND ip.Status = 'Pending' THEN 1 ELSE 0 END AS BIT) AS CanRemovePenalty
    FROM InterviewPoints ip
    LEFT JOIN UserList ul ON ul.id = ip.userid
    WHERE ip.Interviewid = @InterviewId
    ORDER BY ip.Date, ip.id;

    ;WITH H AS (
        SELECT NULLIF(LTRIM(RTRIM(h.interviewer)), '') AS Interviewer,
               h.PerformedAt,
               h.ActionType,
               LAG(NULLIF(LTRIM(RTRIM(h.interviewer)), '')) OVER (ORDER BY h.PerformedAt) AS PrevInterviewer,
               ROW_NUMBER() OVER (ORDER BY h.PerformedAt) AS Rn
        FROM InterviewMeetingsHistory h
        WHERE h.id = @InterviewId
    )
    SELECT
        CASE WHEN Interviewer IS NULL THEN 'Unassigned'
             WHEN PrevInterviewer IS NULL THEN 'Assigned'
             ELSE 'Reassigned' END              AS Change,
        PrevInterviewer                         AS FromInterviewer,
        Interviewer                             AS ToInterviewer,
        PerformedAt
    FROM H
    WHERE ISNULL(Interviewer, '') <> ISNULL(PrevInterviewer, '')
      AND NOT (Rn = 1 AND Interviewer IS NULL)
    ORDER BY PerformedAt;

    SELECT
        l.ActionType,
        l.InterviewPointsId,
        ul.EmailId,
        l.OldPoints,
        l.NewPoints,
        l.Reason,
        l.PerformedBy,
        l.PerformedAt
    FROM OpsToolkitActionLog l
    LEFT JOIN UserList ul ON ul.id = l.UserId
    WHERE l.InterviewId = @InterviewId
    ORDER BY l.Id;
END
GO
