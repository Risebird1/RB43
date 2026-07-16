CREATE OR ALTER PROCEDURE [dbo].[usp_GetTrialFlagBackfillCandidates]
    @MaxRows             INT = 100,
    @ActivityWindowDays  INT = 14   -- BackfillActivityWindowDays: completed interview within this window
AS
BEGIN
    SET NOCOUNT ON;

    IF @MaxRows IS NULL OR @MaxRows < 1 SET @MaxRows = 100;
    IF @MaxRows > 500 SET @MaxRows = 500;
    IF @ActivityWindowDays IS NULL OR @ActivityWindowDays < 1 SET @ActivityWindowDays = 14;
    IF @ActivityWindowDays > 90 SET @ActivityWindowDays = 90;

    IF OBJECT_ID(N'dbo.TrialFlags', N'U') IS NULL
    BEGIN
        SELECT CAST(0 AS INT) AS InterviewId, CAST(0 AS INT) AS IrfId WHERE 1 = 0;
        RETURN;
    END

    DECLARE @ActivitySince DATETIME = DATEADD(DAY, -@ActivityWindowDays, GETUTCDATE());

    /*
      Pair-based candidates (replaces recent-QCDone scan):
        - TransInterviewer.IsActive = 1
        - IRF currently active (GETDATE() between StartDate and EndDate)
        - Pair has >= 1 completed interview (MeetingStatus IN 4,6,7,8) in the last @ActivityWindowDays
        - No TrialFlags row for (InterviewerId, IrfId)
      Triggering interview = the pair's FIRST completed interview on that IRF (may be older than the window).
      Full eligibility (QCDone, merge, etc.) is re-checked by TryEvaluateAndFlagTrialForCompletedMergeAsync.
    */

    ;WITH ActivePairs AS (
        SELECT
            ti.InterviewerId,
            ti.IRFId AS IrfIntId,
            ti.UserId,
            irf.IRFId AS IrfName
        FROM dbo.TransInterviewer ti WITH (NOLOCK)
        INNER JOIN dbo.IRF irf WITH (NOLOCK) ON irf.id = ti.IRFId
        WHERE ISNULL(ti.IsActive, 0) = 1
          AND ti.InterviewerId IS NOT NULL
          AND ti.InterviewerId > 0
          AND GETDATE() BETWEEN ISNULL(irf.StartDate, '1900-01-01') AND ISNULL(irf.EndDate, '2100-01-01')
          AND NOT EXISTS (
                SELECT 1
                FROM dbo.TrialFlags tf WITH (NOLOCK)
                WHERE tf.InterviewerId = ti.InterviewerId
                  AND tf.IrfId = ti.IRFId
              )
    ),
    RecentCompleted AS (
        SELECT DISTINCT
            ap.InterviewerId,
            ap.IrfIntId,
            ap.IrfName
        FROM ActivePairs ap
        INNER JOIN dbo.InterviewMeetings im WITH (NOLOCK) ON im.IRFId = ap.IrfName
        INNER JOIN dbo.InterviewComments ic WITH (NOLOCK) ON ic.InterviewId = im.id
        WHERE ISNULL(im.confirmationStatus, 0) = 1
          AND ic.MeetingStatus IN (4, 6, 7, 8)
          AND im.interviewDate >= @ActivitySince
          AND (
                EXISTS (
                    SELECT 1
                    FROM dbo.UserList ul WITH (NOLOCK)
                    WHERE ul.id = ap.UserId
                      AND (
                            LTRIM(RTRIM(ul.EmailId)) = LTRIM(RTRIM(im.interviewer))
                         OR ul.EmailId = im.interviewer
                          )
                )
             OR EXISTS (
                    SELECT 1
                    FROM dbo.InterviewerProfile ip WITH (NOLOCK)
                    WHERE ip.UserId = ap.UserId
                      AND LTRIM(RTRIM(ISNULL(ip.NickName, ''))) = LTRIM(RTRIM(im.interviewer))
                )
             OR EXISTS (
                    SELECT 1
                    FROM dbo.Interviewer iv WITH (NOLOCK)
                    INNER JOIN dbo.UserList ul WITH (NOLOCK) ON ul.id = iv.UserId
                    WHERE iv.id = ap.InterviewerId
                      AND (
                            LTRIM(RTRIM(ul.EmailId)) = LTRIM(RTRIM(im.interviewer))
                         OR ul.EmailId = im.interviewer
                          )
                )
              )
    ),
    FirstCompleted AS (
        SELECT
            rc.InterviewerId,
            rc.IrfIntId,
            firstIm.InterviewId,
            firstIm.interviewDate
        FROM RecentCompleted rc
        CROSS APPLY (
            SELECT TOP 1
                im.id AS InterviewId,
                im.interviewDate
            FROM dbo.InterviewMeetings im WITH (NOLOCK)
            INNER JOIN dbo.InterviewComments ic WITH (NOLOCK) ON ic.InterviewId = im.id
            WHERE im.IRFId = rc.IrfName
              AND ISNULL(im.confirmationStatus, 0) = 1
              AND ic.MeetingStatus IN (4, 6, 7, 8)
              AND (
                    EXISTS (
                        SELECT 1
                        FROM dbo.UserList ul WITH (NOLOCK)
                        INNER JOIN dbo.TransInterviewer ti WITH (NOLOCK)
                            ON ti.IRFId = rc.IrfIntId AND ti.UserId = ul.id AND ti.InterviewerId = rc.InterviewerId
                        WHERE LTRIM(RTRIM(ul.EmailId)) = LTRIM(RTRIM(im.interviewer))
                           OR ul.EmailId = im.interviewer
                    )
                 OR EXISTS (
                        SELECT 1
                        FROM dbo.InterviewerProfile ip WITH (NOLOCK)
                        INNER JOIN dbo.TransInterviewer ti WITH (NOLOCK)
                            ON ti.IRFId = rc.IrfIntId AND ti.UserId = ip.UserId AND ti.InterviewerId = rc.InterviewerId
                        WHERE LTRIM(RTRIM(ISNULL(ip.NickName, ''))) = LTRIM(RTRIM(im.interviewer))
                    )
                  )
            ORDER BY im.interviewDate ASC, im.id ASC
        ) firstIm
    )
    SELECT TOP (@MaxRows)
        fc.InterviewId,
        fc.IrfIntId AS IrfId
    FROM FirstCompleted fc
    ORDER BY fc.interviewDate DESC, fc.InterviewId DESC;
END
GO
