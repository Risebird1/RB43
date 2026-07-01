CREATE OR ALTER PROCEDURE [dbo].[usp_GetTrialFlagBackfillCandidates]
    @MaxRows  INT = 100,
    @ScanRows INT = 800
AS
BEGIN
    SET NOCOUNT ON;

    IF @MaxRows IS NULL OR @MaxRows < 1 SET @MaxRows = 100;
    IF @MaxRows > 500 SET @MaxRows = 500;
    IF @ScanRows IS NULL OR @ScanRows < @MaxRows SET @ScanRows = @MaxRows * 8;
    IF @ScanRows > 3000 SET @ScanRows = 3000;

    IF OBJECT_ID(N'dbo.TrialFlags', N'U') IS NULL
    BEGIN
        SELECT CAST(0 AS INT) AS InterviewId, CAST(0 AS INT) AS IrfId WHERE 1 = 0;
        RETURN;
    END

    -- Fast path: scan only recent QCDone rows (indexed), then filter in-memory-sized set.
    -- Full eligibility is re-checked per row by usp_GetTrialJudgmentContext during backfill.
    ;WITH RecentQcDone AS (
        SELECT TOP (@ScanRows)
            q.InterviewId,
            q.QCDoneOn
        FROM dbo.QcInterviewAIevaluation q WITH (NOLOCK)
        WHERE ISNULL(q.QCDone, 0) = 1
          AND NOT EXISTS (
              SELECT 1
              FROM dbo.TrialFlags tf WITH (NOLOCK)
              WHERE tf.TriggeringInterviewId = q.InterviewId
          )
        ORDER BY q.QCDoneOn DESC, q.InterviewId DESC
    ),
    CandidateMeetings AS (
        SELECT
            rq.InterviewId,
            rq.QCDoneOn,
            LTRIM(RTRIM(im.interviewer)) AS InterviewerRaw,
            im.interviewer AS InterviewerStored,
            im.interviewDate,
            irf.id AS IrfIntId
        FROM RecentQcDone rq
        INNER JOIN dbo.InterviewMeetings im WITH (NOLOCK) ON im.id = rq.InterviewId
        INNER JOIN dbo.InterviewComments ic WITH (NOLOCK) ON ic.InterviewId = im.id
        INNER JOIN dbo.IRF irf WITH (NOLOCK) ON irf.IRFId = im.IRFId
        WHERE ISNULL(im.confirmationStatus, 0) = 1
          AND (
                ic.MeetingStatus IN (4, 6, 7, 8)
             OR ic.InterviewStatus IN (2, 3, 4, 5)
          )
          AND NOT EXISTS (
              SELECT 1
              FROM dbo.InterviewMeetings im2 WITH (NOLOCK)
              INNER JOIN dbo.InterviewComments ic2 WITH (NOLOCK) ON ic2.InterviewId = im2.id
              WHERE im2.IRFId = im.IRFId
                AND LTRIM(RTRIM(im2.interviewer)) = LTRIM(RTRIM(im.interviewer))
                AND ISNULL(im2.confirmationStatus, 0) = 1
                AND (
                      ic2.MeetingStatus IN (4, 6, 7, 8)
                   OR ic2.InterviewStatus IN (2, 3, 4, 5)
                )
                AND (
                    im2.interviewDate < im.interviewDate
                    OR (im2.interviewDate = im.interviewDate AND im2.id < im.id)
                )
          )
    )
    SELECT TOP (@MaxRows)
        cm.InterviewId,
        cm.IrfIntId AS IrfId
    FROM CandidateMeetings cm
    WHERE EXISTS (
        SELECT 1
        FROM dbo.UserList ul WITH (NOLOCK)
        INNER JOIN dbo.TransInterviewer ti WITH (NOLOCK)
            ON ti.IRFId = cm.IrfIntId
           AND ti.UserId = ul.id
        WHERE ul.EmailId = cm.InterviewerStored
           OR LTRIM(RTRIM(ul.EmailId)) = cm.InterviewerRaw
    )
    OR EXISTS (
        SELECT 1
        FROM dbo.UserList ul WITH (NOLOCK)
        INNER JOIN dbo.Interviewer iv WITH (NOLOCK) ON iv.UserId = ul.id
        INNER JOIN dbo.TransInterviewer ti WITH (NOLOCK)
            ON ti.IRFId = cm.IrfIntId
           AND ti.InterviewerId = iv.id
        WHERE ul.EmailId = cm.InterviewerStored
           OR LTRIM(RTRIM(ul.EmailId)) = cm.InterviewerRaw
    )
    OR EXISTS (
        SELECT 1
        FROM dbo.InterviewerProfile ip WITH (NOLOCK)
        INNER JOIN dbo.UserList ul WITH (NOLOCK) ON ul.id = ip.UserId
        INNER JOIN dbo.TransInterviewer ti WITH (NOLOCK)
            ON ti.IRFId = cm.IrfIntId
           AND ti.UserId = ul.id
        WHERE LTRIM(RTRIM(ISNULL(ip.NickName, ''))) = cm.InterviewerRaw
    )
    ORDER BY cm.QCDoneOn DESC, cm.InterviewId DESC;
END
GO
