/*
================================================================================
usp_GetQcPageStatus — current QC decision per interviewer (and channel) on a QC
page (QC Tracking, Delta 5): the latest RedMarkerStatusDetails row of @Type
(PLI, PotentialRisk, HighRisk) for each requested interviewer.

@Type     PLI | PotentialRisk | HighRisk
@UserIds  comma-separated UserList ids

One row per (UserId, IRFId) that has a decision; IRFId 0 = a decision for the
interviewer as a whole (a Remove through usp_InactivateInterviewer, which makes
the interviewer inactive on every channel). Status 1 Under review, 2 Reviewed,
3 Removed. Read-only.
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_GetQcPageStatus]
    @Type    VARCHAR(30),
    @UserIds NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    IF @Type NOT IN ('PLI', 'PotentialRisk', 'HighRisk')
    BEGIN
        SELECT CAST(0 AS INT) AS UserId, CAST(0 AS INT) AS IRFId, CAST(0 AS INT) AS [Status],
               CAST(NULL AS VARCHAR(200)) AS CreatedBy, CAST(NULL AS DATETIME) AS CreatedOn, CAST(NULL AS VARCHAR(300)) AS Comment
        WHERE 1 = 0;
        RETURN;
    END

    ;WITH Ids AS (
        SELECT DISTINCT TRY_CAST(LTRIM(RTRIM(value)) AS INT) AS UserId
        FROM STRING_SPLIT(ISNULL(@UserIds, N''), N',')
        WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) > 0
    ),
    Ranked AS (
        SELECT r.InterviewerUserid AS UserId, ISNULL(r.IRFId, 0) AS IRFId, r.[Status], r.CreatedBy, r.CreatedOn, r.Comment,
               ROW_NUMBER() OVER (PARTITION BY r.InterviewerUserid, ISNULL(r.IRFId, 0) ORDER BY r.CreatedOn DESC, r.id DESC) AS rn
        FROM dbo.RedMarkerStatusDetails r
        INNER JOIN Ids i ON i.UserId = r.InterviewerUserid
        WHERE r.[Type] = @Type
    )
    SELECT UserId, IRFId, [Status], CreatedBy, CreatedOn, Comment
    FROM Ranked
    WHERE rn = 1;
END
GO
