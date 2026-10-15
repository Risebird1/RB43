/*
================================================================================
usp_GetInterviewerMyQuality — the interviewer's own "My Quality" page (read-only).

Keyed on the session email only (the web action never takes an interviewer id from
the request). Every row is the interviewer's own. Nothing here names a QC person,
another interviewer, a client or a candidate; thresholds and internal labels are
not returned as text (InterviewerMyQualityService translates check codes and
drops never-shown ones before anything reaches the page).

Windows (IST dates; InterviewMeetings.interviewDate is IST, as in
usp_GetInterviewerExposure): current = the last 30 days including today,
previous = the 30 days before that. Completed = confirmationStatus 1 and
MeetingStatus 4, 6, 7, 8.

Result sets:
  1 Tiles       issue-free (no P0-P5 RiskCategory grade) and on-time (first join
                within 5 minutes of the start) for the current and previous 30 days;
                approved deductions this calendar month and last month (count, avg %,
                amount); active and paused channels.
  2 Channels    per-channel issue-free share, current 30 days (IRF code only) + standing.
  3 Issues      RiskCategory rows on completed interviews in the current 30 days
                (check codes are mapped to themes in C#; Trial / Audit / RedMarker /
                auto-QC suspicious standing is never read here).
  4 Deductions  Payment SLA deductions above 0 in the last 60 days: %, wallet amount,
                recommendation, approval state and the reason columns.
  5 Coaching    coaching sessions (latest 20): date, channel, themes, acknowledged at.
  6 Wording     active InterviewerFeedbackText rows (empty when the table is missing).

Amount = latest MoneyProtectionWallet.Points (already deduction % x client interview rate / 100).
Applied date = first wallet row (starts the 7-day review window).
Review state (phase 2) comes from DeductionReviewRequest when that table is deployed.
Optional objects are read behind OBJECT_ID / COL_LENGTH checks.
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_GetInterviewerMyQuality]
    @Email VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    SET @Email = NULLIF(LTRIM(RTRIM(@Email)), '');

    DECLARE @UserId INT = (SELECT TOP 1 id FROM dbo.UserList WHERE EmailId = @Email ORDER BY id DESC);
    DECLARE @ProfileId INT = (SELECT TOP 1 Id FROM dbo.InterviewerProfile WHERE UserId = @UserId ORDER BY Id DESC);

    DECLARE @TodayIst      DATE = CAST(DATEADD(MINUTE, 330, GETUTCDATE()) AS DATE);
    DECLARE @CurFrom       DATETIME = CAST(DATEADD(DAY, -29, @TodayIst) AS DATETIME);
    DECLARE @CurTo         DATETIME = CAST(DATEADD(DAY, 1, @TodayIst) AS DATETIME);
    DECLARE @PrevFrom      DATETIME = DATEADD(DAY, -30, @CurFrom);
    DECLARE @DedFrom       DATETIME = CAST(DATEADD(DAY, -59, @TodayIst) AS DATETIME);
    DECLARE @MonthFrom     DATETIME = DATEFROMPARTS(YEAR(@TodayIst), MONTH(@TodayIst), 1);
    DECLARE @PrevMonthFrom DATETIME = DATEADD(MONTH, -1, @MonthFrom);
    DECLARE @WindowFrom    DATETIME = CASE WHEN @PrevFrom < @PrevMonthFrom THEN @PrevFrom ELSE @PrevMonthFrom END;
    IF @DedFrom < @WindowFrom SET @WindowFrom = @DedFrom;

    /* Interviews by this interviewer in the widest window used below. */
    CREATE TABLE #Iv (
        InterviewId      INT NOT NULL PRIMARY KEY,
        IrfId            INT NULL,
        IrfName          VARCHAR(200) NULL,
        InterviewDateIst DATETIME NULL,
        IsCompleted      BIT NOT NULL,
        HasIssue         BIT NOT NULL,
        FirstJoin        DATETIME NULL
    );

    IF @UserId IS NOT NULL
    BEGIN
        INSERT INTO #Iv (InterviewId, IrfId, IrfName, InterviewDateIst, IsCompleted, HasIssue, FirstJoin)
        SELECT
            im.id,
            irf.id,
            im.IRFId,
            MAX(im.interviewDate),
            CAST(MAX(CASE WHEN im.confirmationStatus = 1 AND ic.MeetingStatus IN (4, 6, 7, 8) THEN 1 ELSE 0 END) AS BIT),
            CAST(0 AS BIT),
            NULL
        FROM dbo.InterviewMeetings im
        LEFT JOIN dbo.InterviewComments ic ON ic.InterviewId = im.id
        OUTER APPLY (SELECT TOP 1 i.id FROM dbo.IRF i WHERE i.IRFId = im.IRFId ORDER BY i.id DESC) irf
        WHERE im.interviewer = @Email
          AND im.interviewDate >= @WindowFrom
          AND im.interviewDate <  @CurTo
        GROUP BY im.id, irf.id, im.IRFId;

        UPDATE iv SET HasIssue = 1
        FROM #Iv iv
        WHERE EXISTS (
            SELECT 1 FROM dbo.RiskCategory rc
            WHERE rc.InterviewId = iv.InterviewId
              AND UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) LIKE 'P[0-5]');

        UPDATE iv SET FirstJoin = j.FirstJoin
        FROM #Iv iv
        CROSS APPLY (
            SELECT MIN(imj.Joinedon) AS FirstJoin
            FROM dbo.InterviewMeetingsJoinTime imj
            WHERE imj.interviewid = iv.InterviewId
        ) j;
    END

    /* Review requests on these interviews (phase 2; empty when the table is not deployed). */
    CREATE TABLE #Rev (
        InterviewId      INT NOT NULL PRIMARY KEY,
        ReviewStatus     VARCHAR(20) NOT NULL,
        ReviewOriginalPct INT NOT NULL,
        ReviewNewPct     INT NULL,
        ReviewRequestedAtUtc DATETIME NOT NULL,
        ReviewDueAtUtc   DATETIME NOT NULL,
        ReviewDecidedAtUtc DATETIME NULL,
        ReviewDecisionNote NVARCHAR(500) NULL
    );
    DECLARE @ReviewsThisMonth INT = 0;

    IF OBJECT_ID(N'dbo.DeductionReviewRequest', N'U') IS NOT NULL AND @UserId IS NOT NULL
    BEGIN
        EXEC sp_executesql N'
            INSERT INTO #Rev (InterviewId, ReviewStatus, ReviewOriginalPct, ReviewNewPct, ReviewRequestedAtUtc,
                              ReviewDueAtUtc, ReviewDecidedAtUtc, ReviewDecisionNote)
            SELECT r.InterviewId, r.Status, r.DeductionPct, r.NewDeductionPct, r.RequestedAtUtc,
                   r.DueAtUtc, r.DecidedAtUtc, r.DecisionNote
            FROM dbo.DeductionReviewRequest r
            INNER JOIN #Iv iv ON iv.InterviewId = r.InterviewId;

            DECLARE @TodayIst DATE = CAST(DATEADD(MINUTE, 330, GETUTCDATE()) AS DATE);
            SELECT @Count = COUNT(1)
            FROM dbo.DeductionReviewRequest r
            WHERE r.InterviewerUserId IN (SELECT id FROM dbo.UserList WHERE EmailId = @Email)
              AND r.RequestedAtUtc >= DATEADD(MINUTE, -330, CAST(DATEFROMPARTS(YEAR(@TodayIst), MONTH(@TodayIst), 1) AS DATETIME));',
            N'@Email VARCHAR(150), @Count INT OUTPUT',
            @Email = @Email, @Count = @ReviewsThisMonth OUTPUT;
    END

    /* Deductions (Payment SLA) in the deduction / month windows. A deduction a review refunded
       (now 0%) stays listed so the interviewer sees the outcome. */
    CREATE TABLE #Ded (
        InterviewId      INT NOT NULL PRIMARY KEY,
        DeductionPct     INT NOT NULL,
        IsApproved       INT NOT NULL,
        Amount           DECIMAL(18,2) NULL,
        AppliedAt        DATETIME NULL
    );

    INSERT INTO #Ded (InterviewId, DeductionPct, IsApproved, Amount, AppliedAt)
    SELECT iv.InterviewId, ps.MaxDeduction, ps.ApprovalState, w.Amount, COALESCE(wf.AddedOn, ps.SavedOn)
    FROM #Iv iv
    CROSS APPLY (
        SELECT
            MAX(ISNULL(p.PaymentDeduction, 0)) AS MaxDeduction,
            -- 1 approved (applied), 2 not approved, 0 still being checked
            CASE WHEN MAX(CASE WHEN ISNULL(p.IsApproved, 0) = 1 THEN 1 ELSE 0 END) = 1 THEN 1
                 WHEN MAX(CASE WHEN ISNULL(p.IsApproved, 0) = 2 THEN 1 ELSE 0 END) = 1 THEN 2
                 ELSE 0 END AS ApprovalState,
            MAX(COALESCE(p.ModifiedOn, p.CreatedOn)) AS SavedOn
        FROM dbo.PaymentSLADetails p
        WHERE p.Interviewid = iv.InterviewId
        HAVING MAX(ISNULL(p.PaymentDeduction, 0)) > 0
            OR EXISTS (SELECT 1 FROM #Rev rv WHERE rv.InterviewId = iv.InterviewId)
    ) ps
    OUTER APPLY (
        -- Current amount: latest wallet row. Points already holds deduction % x client interview rate / 100.
        SELECT TOP 1 CAST(ISNULL(mw.Points, 0) AS DECIMAL(18,2)) AS Amount
        FROM dbo.MoneyProtectionWallet mw
        WHERE mw.Interviewid = iv.InterviewId
        ORDER BY mw.id DESC
    ) w
    OUTER APPLY (
        -- Applied date: the first wallet row, written on approval (starts the 7-day review window).
        SELECT TOP 1 mw.AddedOn
        FROM dbo.MoneyProtectionWallet mw
        WHERE mw.Interviewid = iv.InterviewId
        ORDER BY mw.id ASC
    ) wf;

    /* Channels: active, and paused (removed by audit / auto-QC, or red-marked). */
    CREATE TABLE #Ch (IrfId INT NOT NULL PRIMARY KEY, IrfName VARCHAR(200) NULL, IsActive BIT NOT NULL, IsPaused BIT NOT NULL);

    IF @UserId IS NOT NULL
    BEGIN
        INSERT INTO #Ch (IrfId, IrfName, IsActive, IsPaused)
        SELECT t.IRFId, MAX(irf.IRFId),
               CAST(MAX(CASE WHEN ISNULL(t.IsActive, 0) = 1 THEN 1 ELSE 0 END) AS BIT),
               CAST(0 AS BIT)
        FROM dbo.TransInterviewer t
        LEFT JOIN dbo.IRF irf ON irf.id = t.IRFId
        WHERE t.UserId = @UserId AND t.IRFId IS NOT NULL
        GROUP BY t.IRFId;

        IF OBJECT_ID(N'dbo.TransInterviewerAuditLog', N'U') IS NOT NULL
            EXEC sp_executesql N'
                UPDATE c SET IsPaused = 1
                FROM #Ch c
                WHERE c.IsActive = 0
                  AND EXISTS (
                    SELECT 1 FROM dbo.TransInterviewerAuditLog l
                    INNER JOIN dbo.TransInterviewer t ON t.id = l.TransInterviewerId
                    WHERE t.UserId = @UserId AND t.IRFId = c.IrfId
                      AND l.EventType IN (''RiskAuditRemoval'', ''AutoQcRemoval''))',
                N'@UserId INT', @UserId = @UserId;

        IF @ProfileId IS NOT NULL
            UPDATE c SET IsPaused = 1
            FROM #Ch c
            WHERE c.IsActive = 0
              AND EXISTS (
                SELECT 1 FROM dbo.TransInterviewerRedMarker trm
                WHERE trm.InterviewerId = @ProfileId AND trm.IRFId = c.IrfId AND trm.RedMarker > 0);
    END

    /* ---------- 1 Tiles ---------- */
    DECLARE @CurCompleted INT, @CurIssueFree INT, @PrevCompleted INT, @PrevIssueFree INT,
            @CurJoinTracked INT, @CurOnTime INT, @PrevJoinTracked INT, @PrevOnTime INT;

    SELECT
        @CurCompleted    = SUM(CASE WHEN iv.InterviewDateIst >= @CurFrom THEN 1 ELSE 0 END),
        @CurIssueFree    = SUM(CASE WHEN iv.InterviewDateIst >= @CurFrom AND iv.HasIssue = 0 THEN 1 ELSE 0 END),
        @PrevCompleted   = SUM(CASE WHEN iv.InterviewDateIst >= @PrevFrom AND iv.InterviewDateIst < @CurFrom THEN 1 ELSE 0 END),
        @PrevIssueFree   = SUM(CASE WHEN iv.InterviewDateIst >= @PrevFrom AND iv.InterviewDateIst < @CurFrom AND iv.HasIssue = 0 THEN 1 ELSE 0 END),
        @CurJoinTracked  = SUM(CASE WHEN iv.InterviewDateIst >= @CurFrom AND iv.FirstJoin IS NOT NULL THEN 1 ELSE 0 END),
        @CurOnTime       = SUM(CASE WHEN iv.InterviewDateIst >= @CurFrom AND iv.FirstJoin IS NOT NULL
                                     AND DATEDIFF(MINUTE, iv.InterviewDateIst, iv.FirstJoin) <= 5 THEN 1 ELSE 0 END),
        @PrevJoinTracked = SUM(CASE WHEN iv.InterviewDateIst >= @PrevFrom AND iv.InterviewDateIst < @CurFrom AND iv.FirstJoin IS NOT NULL THEN 1 ELSE 0 END),
        @PrevOnTime      = SUM(CASE WHEN iv.InterviewDateIst >= @PrevFrom AND iv.InterviewDateIst < @CurFrom AND iv.FirstJoin IS NOT NULL
                                     AND DATEDIFF(MINUTE, iv.InterviewDateIst, iv.FirstJoin) <= 5 THEN 1 ELSE 0 END)
    FROM #Iv iv
    WHERE iv.IsCompleted = 1;

    DECLARE @MonthCount INT, @MonthAvg DECIMAL(10,2), @MonthAmount DECIMAL(18,2), @PrevMonthCount INT;

    SELECT
        @MonthCount     = SUM(CASE WHEN x.InterviewDateIst >= @MonthFrom THEN 1 ELSE 0 END),
        @MonthAvg       = CAST(AVG(CASE WHEN x.InterviewDateIst >= @MonthFrom THEN CAST(d.DeductionPct AS DECIMAL(10,2)) END) AS DECIMAL(10,2)),
        @MonthAmount    = SUM(CASE WHEN x.InterviewDateIst >= @MonthFrom THEN d.Amount END),
        @PrevMonthCount = SUM(CASE WHEN x.InterviewDateIst >= @PrevMonthFrom AND x.InterviewDateIst < @MonthFrom THEN 1 ELSE 0 END)
    FROM #Ded d
    INNER JOIN #Iv x ON x.InterviewId = d.InterviewId
    WHERE d.IsApproved = 1 AND d.DeductionPct > 0;

    SELECT
        ISNULL(@CurCompleted, 0)    AS CurCompleted,
        ISNULL(@CurIssueFree, 0)    AS CurIssueFree,
        ISNULL(@PrevCompleted, 0)   AS PrevCompleted,
        ISNULL(@PrevIssueFree, 0)   AS PrevIssueFree,
        ISNULL(@CurJoinTracked, 0)  AS CurJoinTracked,
        ISNULL(@CurOnTime, 0)       AS CurOnTime,
        ISNULL(@PrevJoinTracked, 0) AS PrevJoinTracked,
        ISNULL(@PrevOnTime, 0)      AS PrevOnTime,
        ISNULL(@MonthCount, 0)      AS MonthDeductionCount,
        @MonthAvg                   AS MonthDeductionAvgPct,
        @MonthAmount                AS MonthDeductionAmount,
        ISNULL(@PrevMonthCount, 0)  AS PrevMonthDeductionCount,
        (SELECT COUNT(1) FROM #Ch WHERE IsActive = 1) AS ActiveChannels,
        (SELECT COUNT(1) FROM #Ch WHERE IsActive = 0 AND IsPaused = 1) AS PausedChannels,
        ISNULL(@ReviewsThisMonth, 0) AS ReviewRequestsThisMonth;

    /* ---------- 2 Per-channel issue-free share (current 30 days) ---------- */
    SELECT
        iv.IrfId,
        MAX(iv.IrfName) AS IrfName,
        COUNT(1) AS Completed,
        SUM(CASE WHEN iv.HasIssue = 0 THEN 1 ELSE 0 END) AS IssueFree,
        CASE WHEN MAX(CAST(c.IsActive AS INT)) = 1 THEN 'Active'
             WHEN MAX(CAST(c.IsPaused AS INT)) = 1 THEN 'Paused'
             ELSE '' END AS Standing
    FROM #Iv iv
    LEFT JOIN #Ch c ON c.IrfId = iv.IrfId
    WHERE iv.IsCompleted = 1 AND iv.InterviewDateIst >= @CurFrom
    GROUP BY iv.IrfId
    ORDER BY COUNT(1) DESC, MAX(iv.IrfName);

    /* ---------- 3 Issues: RiskCategory rows, current 30 days ---------- */
    SELECT iv.InterviewId, iv.IrfId, iv.IrfName, iv.InterviewDateIst,
           rc.SeverityBreakdown, rc.[Description], rc.Severity
    FROM #Iv iv
    INNER JOIN dbo.RiskCategory rc ON rc.InterviewId = iv.InterviewId
    WHERE iv.IsCompleted = 1
      AND iv.InterviewDateIst >= @CurFrom
      AND (NULLIF(LTRIM(RTRIM(ISNULL(rc.SeverityBreakdown, ''))), '') IS NOT NULL
        OR NULLIF(LTRIM(RTRIM(ISNULL(rc.[Description], ''))), '') IS NOT NULL)
    ORDER BY iv.InterviewDateIst DESC;

    /* ---------- 4 Deductions, last 60 days ---------- */
    SELECT iv.InterviewId, iv.IrfId, iv.IrfName, iv.InterviewDateIst,
           d.DeductionPct AS PaymentDeduction, d.Amount, d.IsApproved AS ApprovalState,
           ps.Recommendation,
           ps.Inaccuratedecisionmaking, ps.TimeManagement, ps.QuestionsRelevance, ps.ScoreRubricIgnorance,
           ps.PoorFeedbackComments, ps.CandidateInteractionQuality, ps.LimitedHandoncoding, ps.WrongStatusUpdates,
           ps.WeakInfrastructure, ps.ProctoringIgnorance, ps.UnethicalPractices, ps.PoorCommunicationSkills,
           ps.AttendanceJoinCompliance, ps.InterviewerVideo,
           d.AppliedAt,
           rv.ReviewStatus, rv.ReviewOriginalPct, rv.ReviewNewPct, rv.ReviewRequestedAtUtc,
           rv.ReviewDueAtUtc, rv.ReviewDecidedAtUtc, rv.ReviewDecisionNote
    FROM #Ded d
    INNER JOIN #Iv iv ON iv.InterviewId = d.InterviewId
    LEFT JOIN #Rev rv ON rv.InterviewId = d.InterviewId
    OUTER APPLY (
        SELECT TOP 1 p.*
        FROM dbo.PaymentSLADetails p
        WHERE p.Interviewid = d.InterviewId
        ORDER BY ISNULL(p.PaymentDeduction, 0) DESC, p.id DESC
    ) ps
    WHERE iv.InterviewDateIst >= @DedFrom
    ORDER BY iv.InterviewDateIst DESC;

    /* ---------- 5 Coaching sessions ---------- */
    IF OBJECT_ID(N'dbo.InterviewerCoachingSession', N'U') IS NOT NULL AND @UserId IS NOT NULL
    BEGIN
        IF COL_LENGTH('dbo.InterviewerCoachingSession', 'AcknowledgedAtUtc') IS NOT NULL
            EXEC sp_executesql N'
                SELECT TOP 20 s.Id AS SessionId, s.CreatedAtUtc, s.Channel, s.Themes, s.AcknowledgedAtUtc
                FROM dbo.InterviewerCoachingSession s
                WHERE s.InterviewerUserId IN (SELECT id FROM dbo.UserList WHERE EmailId = @Email)
                ORDER BY s.CreatedAtUtc DESC, s.Id DESC',
                N'@Email VARCHAR(150)', @Email = @Email;
        ELSE
            EXEC sp_executesql N'
                SELECT TOP 20 s.Id AS SessionId, s.CreatedAtUtc, s.Channel, s.Themes, CAST(NULL AS DATETIME) AS AcknowledgedAtUtc
                FROM dbo.InterviewerCoachingSession s
                WHERE s.InterviewerUserId IN (SELECT id FROM dbo.UserList WHERE EmailId = @Email)
                ORDER BY s.CreatedAtUtc DESC, s.Id DESC',
                N'@Email VARCHAR(150)', @Email = @Email;
    END
    ELSE
        SELECT CAST(NULL AS INT) AS SessionId, CAST(NULL AS DATETIME) AS CreatedAtUtc, CAST(NULL AS VARCHAR(20)) AS Channel,
               CAST(NULL AS NVARCHAR(1000)) AS Themes, CAST(NULL AS DATETIME) AS AcknowledgedAtUtc
        WHERE 1 = 0;

    /* ---------- 6 Wording ---------- */
    IF OBJECT_ID(N'dbo.InterviewerFeedbackText', N'U') IS NOT NULL
        EXEC sp_executesql N'
            SELECT ThemeKey, Title, WhatToDo
            FROM dbo.InterviewerFeedbackText
            WHERE IsActive = 1';
    ELSE
        SELECT CAST(NULL AS VARCHAR(50)) AS ThemeKey, CAST(NULL AS NVARCHAR(200)) AS Title, CAST(NULL AS NVARCHAR(500)) AS WhatToDo
        WHERE 1 = 0;

    DROP TABLE IF EXISTS #Ch;
    DROP TABLE IF EXISTS #Ded;
    DROP TABLE IF EXISTS #Rev;
    DROP TABLE IF EXISTS #Iv;
END
GO
