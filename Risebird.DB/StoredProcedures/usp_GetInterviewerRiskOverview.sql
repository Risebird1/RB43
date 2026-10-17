/*
================================================================================
usp_GetInterviewerRiskOverview — interviewer-first High-Risk list for IRF Audit.
One row per canonical UserId (identity resolution matches
usp_GetIrfAuditInterviewerRiskBreakdown). Aggregates are pooled over interviews
(TotalBad/TotalCompleted), never an average of per-channel ratios.

Thresholds MUST stay in sync with usp_GetIrfAuditInterviewerRiskBreakdown
(@MinVolume / @RemoveRatio / @HighSevRatio_Rem / @WatchRatio) and
DmIvAutoQcAuditConstants.IrfAudit* in C#.

Default filter (@MinRiskRatio not NULL): at least @MinVolume (3) completed
interviews in the window AND (pooled ratio >= @MinRiskRatio OR any P0), so a
single bad interview out of one is not listed as 100% risk. @MinRiskRatio =
NULL ("Show all") lifts every filter.

Result set 1: page of interviewer rows (+ TotalRows on each row), paged through #Page
              (same sort as before). Profile, activity, lifetime (same definitions as
              usp_GetInterviewerInsights) and window AI-penalty stats are computed for
              the page rows only.
Result set 2: summary header (InterviewersAboveThreshold, ChannelsAffected, PendingReviewInterviewers)
              + AI penalties applied automatically (#AiPen) over every completed
              interview in scope and window
              + deduction review requests received in the window on in-scope channels:
              received, decided, upheld (Reduced + Refunded), upheld %, refunded amount.

Timeout fix (QC Tracking Fix 4A):
  - #Completed filters interviewDate as a range on the column (the same IST window
    shifted to UTC), so the interviewDate index is used; OPTION (RECOMPILE).
  - #AiPen is set-based: one grouped pass over PaymentSLADetails joined to the window
    and one semi-join pass over PaymentSLADetailsHistory. MoneyProtectionWallet is no
    longer read: the AiAmount column is gone from result sets 1 and 2 (the wallet is
    still written by usp_AddorUpdatePaymentSLADetails; only this page stopped showing it).
  - Indexes in Indexes/QCActivityPerformanceIndexes.sql serve the per-row lookups.

QC Tracking additions (appended at the end of result set 1; every new parameter defaults to NULL):
  HighRiskStatus / HighRiskStatusBy / HighRiskStatusOn / HighRiskStatusComment
             latest RedMarkerStatusDetails row of Type HighRisk (1 Under review, 2 Reviewed, 3 Removed)
  Impact     bad interviews x severity weight (P0 = 5, P1 = 3, P2 = 2, P3-P5 = 1)
  ChannelsTotal / ChannelsRemoved   in-scope channels the interviewer is on / inactive on
  PrevCompleted / PrevBad           the window of the same length just before @FromIst (Trend)
  InterviewerIsActive
@SortBy     NULL / 'Impact' (default, decision D1) | 'Ratio' (the earlier order: worst severity, ratio)
@NeedsCall  1 = only interviewers with no connected call (CallRecordingData to the interviewer, at least
            QCActivitySetting.MinCallSeconds, answered) in the window ("Needs a call", Delta 6)
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_GetInterviewerRiskOverview]
    @FromIst           DATE = NULL,
    @ToIst             DATE = NULL,
    @OwnerEmail        VARCHAR(200) = NULL,
    @MinRiskRatio      DECIMAL(5,2) = 0.25,  -- NULL = show all (no ratio/P0 default filter)
    @PendingReviewOnly BIT = 0,
    @Offset            INT = 0,
    @PageSize          INT = 25,
    @IdsOnly           BIT = 0,  -- 1: one result set with every listed interviewer (no paging, no extra stats).
                                 -- Used by usp_QCActivity_HighRisk so QC Activity coverage uses this exact list.
    @SortBy            VARCHAR(20) = NULL,  -- NULL / 'Impact': Impact first (D1); 'Ratio': worst severity, then ratio
    @NeedsCall         BIT = NULL           -- 1: only interviewers with no connected call in the window
AS
BEGIN
    SET NOCOUNT ON;

    -- Same thresholds as usp_GetIrfAuditInterviewerRiskBreakdown (do not diverge)
    DECLARE @MinVolume        INT = 3;
    DECLARE @RemoveRatio      DECIMAL(10,4) = 0.50;
    DECLARE @HighSevRatio_Rem DECIMAL(10,4) = 0.25;
    DECLARE @WatchRatio       DECIMAL(10,4) = 0.25;

    IF @ToIst IS NULL
        SET @ToIst = CAST(DATEADD(MINUTE, 330, GETUTCDATE()) AS DATE);
    IF @FromIst IS NULL
        SET @FromIst = DATEADD(DAY, -30, @ToIst);
    IF @FromIst > @ToIst
        SET @FromIst = DATEADD(DAY, -30, @ToIst);
    IF DATEDIFF(DAY, @FromIst, @ToIst) > 90
        SET @FromIst = DATEADD(DAY, -90, @ToIst);

    IF @Offset IS NULL OR @Offset < 0 SET @Offset = 0;
    IF @PageSize IS NULL OR @PageSize < 1 SET @PageSize = 25;
    IF @PageSize > 200 SET @PageSize = 200;
    SET @SortBy = CASE WHEN LTRIM(RTRIM(ISNULL(@SortBy, ''))) = 'Ratio' THEN 'Ratio' ELSE 'Impact' END;

    -- The IST window as a half-open UTC range on InterviewMeetings.interviewDate (index-friendly).
    DECLARE @FromUtc DATETIME = DATEADD(MINUTE, -330, CAST(@FromIst AS DATETIME));
    DECLARE @ToUtc   DATETIME = DATEADD(MINUTE, -330, DATEADD(DAY, 1, CAST(@ToIst AS DATETIME)));
    -- Trend: the window of the same length just before this one.
    DECLARE @PrevFromUtc DATETIME = DATEADD(DAY, -(DATEDIFF(DAY, @FromIst, @ToIst) + 1), @FromUtc);

    IF OBJECT_ID(N'dbo.RiskCategory', N'U') IS NULL
    BEGIN
        RAISERROR('dbo.RiskCategory is missing.', 16, 1);
        RETURN;
    END

    DECLARE @HasRiskBackfillTable BIT = CASE WHEN OBJECT_ID(N'dbo.IrfAuditRiskBackfillRun', N'U') IS NOT NULL THEN 1 ELSE 0 END;
    DECLARE @OwnerFilterActive BIT = CASE
        WHEN NULLIF(LTRIM(RTRIM(@OwnerEmail)), '') IS NOT NULL THEN 1 ELSE 0 END;

    CREATE TABLE #ScopedIrfs (
        IrfId   INT NOT NULL PRIMARY KEY,
        IrfName VARCHAR(200) NOT NULL
    );

    INSERT INTO #ScopedIrfs (IrfId, IrfName)
    SELECT irf.id, irf.IRFId
    FROM dbo.IRF irf
    LEFT JOIN dbo.UserList ulco ON ulco.id = irf.RisebirdHr
    WHERE NULLIF(LTRIM(RTRIM(irf.IRFId)), '') IS NOT NULL
      AND (
            @OwnerFilterActive = 0
         OR LTRIM(RTRIM(ulco.EmailId)) = LTRIM(RTRIM(@OwnerEmail))
          );

    CREATE INDEX IX_ScopedIrfs_Name ON #ScopedIrfs(IrfName);

    CREATE TABLE #ActiveTi (
        TransInterviewerId INT NOT NULL PRIMARY KEY,
        IrfId              INT NOT NULL,
        UserId             INT NULL,
        InterviewerEmail   VARCHAR(200) NULL,
        InterviewerCodedId VARCHAR(50) NULL
    );

    INSERT INTO #ActiveTi (TransInterviewerId, IrfId, UserId, InterviewerEmail, InterviewerCodedId)
    SELECT
        ti.id,
        ti.IRFId,
        COALESCE(ti.UserId, iv.UserId, ipr.UserId),
        NULLIF(LTRIM(RTRIM(COALESCE(ulTi.EmailId, ulIv.EmailId, ulIp.EmailId))), ''),
        NULLIF(LTRIM(RTRIM(COALESCE(ipTi.NickName, ipr.NickName, ipUser.NickName))), '')
    FROM dbo.TransInterviewer ti
    INNER JOIN #ScopedIrfs s ON s.IrfId = ti.IRFId
    LEFT JOIN dbo.Interviewer iv ON iv.id = ti.InterviewerId
    LEFT JOIN dbo.InterviewerProfile ipr ON ipr.id = ti.InterviewerId
    LEFT JOIN dbo.UserList ulTi ON ulTi.id = ti.UserId
    LEFT JOIN dbo.UserList ulIv ON ulIv.id = iv.UserId
    LEFT JOIN dbo.UserList ulIp ON ulIp.id = ipr.UserId
    LEFT JOIN dbo.InterviewerProfile ipTi ON ipTi.UserId = ti.UserId
    LEFT JOIN dbo.InterviewerProfile ipUser ON ipUser.UserId = COALESCE(ti.UserId, iv.UserId, ipr.UserId)
    WHERE ISNULL(ti.IsActive, 0) = 1;

    DELETE FROM #ActiveTi WHERE UserId IS NULL;

    CREATE INDEX IX_ActiveTi_UserId ON #ActiveTi(UserId);
    CREATE INDEX IX_ActiveTi_Email ON #ActiveTi(InterviewerEmail);
    CREATE INDEX IX_ActiveTi_Coded ON #ActiveTi(InterviewerCodedId);
    CREATE INDEX IX_ActiveTi_Irf ON #ActiveTi(IrfId);

    CREATE TABLE #Completed (
        InterviewId      INT NOT NULL PRIMARY KEY,
        IrfId            INT NOT NULL,
        InterviewerRaw   VARCHAR(200) NOT NULL,
        InterviewDateUtc DATETIME NULL
    );

    INSERT INTO #Completed (InterviewId, IrfId, InterviewerRaw, InterviewDateUtc)
    SELECT
        im.id,
        s.IrfId,
        LTRIM(RTRIM(im.interviewer)),
        im.interviewDate
    FROM dbo.InterviewMeetings im
    INNER JOIN dbo.InterviewComments ic ON ic.InterviewId = im.id
    INNER JOIN #ScopedIrfs s ON s.IrfName = im.IRFId
    WHERE ic.MeetingStatus IN (4, 6, 7, 8)
      -- Same IST calendar window as CAST(DATEADD(MINUTE, 330, interviewDate) AS DATE) BETWEEN @FromIst AND @ToIst,
      -- written as a range on the column so the interviewDate index is used (Fix 4A).
      AND im.interviewDate >= @FromUtc
      AND im.interviewDate <  @ToUtc
      AND NULLIF(LTRIM(RTRIM(im.interviewer)), '') IS NOT NULL
    OPTION (RECOMPILE);

    CREATE INDEX IX_Completed_Raw ON #Completed(InterviewerRaw);
    CREATE INDEX IX_Completed_Irf ON #Completed(IrfId);

    CREATE TABLE #Mapped (
        UserId      INT NOT NULL,
        IrfId       INT NOT NULL,
        InterviewId INT NOT NULL,
        PRIMARY KEY (UserId, IrfId, InterviewId)
    );

    INSERT INTO #Mapped (UserId, IrfId, InterviewId)
    SELECT DISTINCT a.UserId, c.IrfId, c.InterviewId
    FROM #Completed c
    INNER JOIN dbo.UserList ul ON ul.EmailId = c.InterviewerRaw
    INNER JOIN #ActiveTi a ON a.UserId = ul.id AND a.IrfId = c.IrfId;

    INSERT INTO #Mapped (UserId, IrfId, InterviewId)
    SELECT DISTINCT a.UserId, c.IrfId, c.InterviewId
    FROM #Completed c
    INNER JOIN dbo.UserList ul ON LTRIM(RTRIM(ul.EmailId)) = c.InterviewerRaw
    INNER JOIN #ActiveTi a ON a.UserId = ul.id AND a.IrfId = c.IrfId
    WHERE NOT EXISTS (
        SELECT 1 FROM #Mapped x
        WHERE x.UserId = a.UserId AND x.IrfId = c.IrfId AND x.InterviewId = c.InterviewId);

    INSERT INTO #Mapped (UserId, IrfId, InterviewId)
    SELECT DISTINCT a.UserId, c.IrfId, c.InterviewId
    FROM #Completed c
    INNER JOIN #ActiveTi a ON a.IrfId = c.IrfId AND a.InterviewerEmail = c.InterviewerRaw
    WHERE NOT EXISTS (
        SELECT 1 FROM #Mapped x
        WHERE x.UserId = a.UserId AND x.IrfId = c.IrfId AND x.InterviewId = c.InterviewId);

    INSERT INTO #Mapped (UserId, IrfId, InterviewId)
    SELECT DISTINCT a.UserId, c.IrfId, c.InterviewId
    FROM #Completed c
    INNER JOIN #ActiveTi a ON a.IrfId = c.IrfId AND a.InterviewerCodedId = c.InterviewerRaw
    WHERE NOT EXISTS (
        SELECT 1 FROM #Mapped x
        WHERE x.UserId = a.UserId AND x.IrfId = c.IrfId AND x.InterviewId = c.InterviewId);

    INSERT INTO #Mapped (UserId, IrfId, InterviewId)
    SELECT DISTINCT a.UserId, c.IrfId, c.InterviewId
    FROM #Completed c
    INNER JOIN dbo.InterviewerProfile ip ON LTRIM(RTRIM(ip.NickName)) = c.InterviewerRaw
    INNER JOIN #ActiveTi a ON a.UserId = ip.UserId AND a.IrfId = c.IrfId
    WHERE ip.UserId IS NOT NULL
      AND NOT EXISTS (
        SELECT 1 FROM #Mapped x
        WHERE x.UserId = a.UserId AND x.IrfId = c.IrfId AND x.InterviewId = c.InterviewId);

    CREATE TABLE #ChannelReview (
        UserId          INT NOT NULL,
        IrfId           INT NOT NULL,
        Completed       INT NOT NULL,
        Bad             INT NOT NULL,
        RiskRatio       DECIMAL(10,4) NULL,
        Recommendation  VARCHAR(20) NOT NULL,
        P0Count INT NOT NULL, P1Count INT NOT NULL, P2Count INT NOT NULL,
        P3Count INT NOT NULL, P4Count INT NOT NULL, P5Count INT NOT NULL,
        ReviewState     VARCHAR(20) NOT NULL,
        PRIMARY KEY (UserId, IrfId)
    );

    ;WITH ChannelStats AS (
        SELECT
            m.UserId,
            m.IrfId,
            COUNT(DISTINCT m.InterviewId) AS Completed,
            COUNT(DISTINCT CASE WHEN rc.InterviewId IS NOT NULL
                AND UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) LIKE 'P[0-5]'
                THEN m.InterviewId END) AS Bad,
            COUNT(DISTINCT CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) = 'P0' THEN m.InterviewId END) AS P0Count,
            COUNT(DISTINCT CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) = 'P1' THEN m.InterviewId END) AS P1Count,
            COUNT(DISTINCT CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) = 'P2' THEN m.InterviewId END) AS P2Count,
            COUNT(DISTINCT CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) = 'P3' THEN m.InterviewId END) AS P3Count,
            COUNT(DISTINCT CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) = 'P4' THEN m.InterviewId END) AS P4Count,
            COUNT(DISTINCT CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) = 'P5' THEN m.InterviewId END) AS P5Count
        FROM #Mapped m
        LEFT JOIN dbo.RiskCategory rc ON rc.InterviewId = m.InterviewId
        GROUP BY m.UserId, m.IrfId
        HAVING COUNT(DISTINCT m.InterviewId) > 0
    ),
    ChannelWithRatio AS (
        SELECT
            cs.*,
            CAST(CASE WHEN cs.Completed = 0 THEN NULL
                      ELSE CAST(cs.Bad AS DECIMAL(18,4)) / NULLIF(cs.Completed, 0)
                 END AS DECIMAL(10,4)) AS RiskRatio,
            CASE
                WHEN cs.Completed < @MinVolume THEN 'InsufficientData'
                WHEN (
                        CAST(cs.Bad AS DECIMAL(18,4)) / NULLIF(cs.Completed, 0) >= @RemoveRatio
                     OR CAST(cs.P0Count + cs.P1Count AS DECIMAL(18,4)) / NULLIF(cs.Completed, 0) >= @HighSevRatio_Rem
                     ) THEN 'Remove'
                WHEN (
                        CAST(cs.Bad AS DECIMAL(18,4)) / NULLIF(cs.Completed, 0) >= @WatchRatio
                     OR cs.P0Count >= 1
                     ) THEN 'Watch'
                ELSE 'OK'
            END AS Recommendation
        FROM ChannelStats cs
    ),
    LatestReview AS (
        SELECT
            l.TransInterviewerId, l.EventType, l.ChangedAt,
            ROW_NUMBER() OVER (PARTITION BY l.TransInterviewerId ORDER BY l.ChangedAt DESC, l.Id DESC) AS rn
        FROM dbo.TransInterviewerAuditLog l
        INNER JOIN #ActiveTi a ON a.TransInterviewerId = l.TransInterviewerId
        WHERE l.EventType IN ('RiskAuditRemoval', 'RiskAuditKept')
    ),
    ReviewPick AS (
        SELECT a.UserId, a.IrfId, lr.EventType, lr.ChangedAt
        FROM #ActiveTi a
        INNER JOIN LatestReview lr ON lr.TransInterviewerId = a.TransInterviewerId AND lr.rn = 1
    ),
    BadAfterKeep AS (
        SELECT DISTINCT rp.UserId, rp.IrfId
        FROM ReviewPick rp
        INNER JOIN #Mapped m ON m.UserId = rp.UserId AND m.IrfId = rp.IrfId
        INNER JOIN #Completed c ON c.InterviewId = m.InterviewId
        INNER JOIN dbo.RiskCategory rc ON rc.InterviewId = m.InterviewId
        WHERE rp.EventType = 'RiskAuditKept'
          AND UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) LIKE 'P[0-5]'
          AND c.InterviewDateUtc > rp.ChangedAt
    )
    INSERT INTO #ChannelReview (
        UserId, IrfId, Completed, Bad, RiskRatio, Recommendation,
        P0Count, P1Count, P2Count, P3Count, P4Count, P5Count, ReviewState)
    SELECT
        cwr.UserId, cwr.IrfId, cwr.Completed, cwr.Bad, cwr.RiskRatio, cwr.Recommendation,
        cwr.P0Count, cwr.P1Count, cwr.P2Count, cwr.P3Count, cwr.P4Count, cwr.P5Count,
        CASE
            WHEN rp.EventType = 'RiskAuditKept' AND bak.UserId IS NOT NULL THEN 'NeedsReReview'
            WHEN rp.EventType = 'RiskAuditKept' THEN 'Kept'
            WHEN cwr.Recommendation IN ('Remove', 'Watch') THEN 'PendingReview'
            ELSE 'None'
        END
    FROM ChannelWithRatio cwr
    LEFT JOIN ReviewPick rp ON rp.UserId = cwr.UserId AND rp.IrfId = cwr.IrfId
    LEFT JOIN BadAfterKeep bak ON bak.UserId = cwr.UserId AND bak.IrfId = cwr.IrfId;

    CREATE TABLE #CoveringBackfill (IrfId INT NOT NULL PRIMARY KEY);

    IF @HasRiskBackfillTable = 1
    BEGIN
        INSERT INTO #CoveringBackfill (IrfId)
        SELECT DISTINCT r.IrfId
        FROM dbo.IrfAuditRiskBackfillRun r
        INNER JOIN #ScopedIrfs s ON s.IrfId = r.IrfId
        WHERE r.FromIst <= @FromIst AND r.ToIst >= @ToIst;
    END

    CREATE TABLE #Filtered (
        InterviewerId          INT NOT NULL PRIMARY KEY,
        ChannelsActive         INT NOT NULL,
        TotalCompleted         INT NOT NULL,
        TotalBad               INT NOT NULL,
        OverallRiskRatio       DECIMAL(10,4) NULL,
        WorstSeverity          VARCHAR(4) NULL,
        PriorityTier          VARCHAR(20) NULL,
        HighRiskChannels       INT NOT NULL,
        PendingReviewChannels  INT NOT NULL,
        CoverageIncomplete     BIT NOT NULL,
        P0Count INT NOT NULL, P1Count INT NOT NULL, P2Count INT NOT NULL,
        P3Count INT NOT NULL, P4Count INT NOT NULL, P5Count INT NOT NULL,
        -- Impact: bad interviews x severity weight (P0 = 5, P1 = 3, P2 = 2, P3-P5 = 1)
        Impact AS (P0Count * 5 + P1Count * 3 + P2Count * 2 + P3Count + P4Count + P5Count)
    );

    INSERT INTO #Filtered (
        InterviewerId, ChannelsActive, TotalCompleted, TotalBad, OverallRiskRatio,
        WorstSeverity, PriorityTier, HighRiskChannels, PendingReviewChannels, CoverageIncomplete,
        P0Count, P1Count, P2Count, P3Count, P4Count, P5Count)
    SELECT
        cr.UserId,
        COUNT(DISTINCT cr.IrfId),
        SUM(cr.Completed),
        SUM(cr.Bad),
        CAST(CASE WHEN SUM(cr.Completed) = 0 THEN NULL
                  ELSE CAST(SUM(cr.Bad) AS DECIMAL(18,4)) / NULLIF(SUM(cr.Completed), 0)
             END AS DECIMAL(10,4)),
        CASE
            WHEN SUM(cr.P0Count) > 0 THEN 'P0'
            WHEN SUM(cr.P1Count) > 0 THEN 'P1'
            WHEN SUM(cr.P2Count) > 0 THEN 'P2'
            WHEN SUM(cr.P3Count) > 0 THEN 'P3'
            WHEN SUM(cr.P4Count) > 0 THEN 'P4'
            WHEN SUM(cr.P5Count) > 0 THEN 'P5'
            ELSE NULL
        END,
        CASE
            WHEN SUM(cr.P0Count) > 0 OR SUM(cr.P1Count) > 0 THEN 'Critical'
            WHEN SUM(cr.P2Count) > 0 OR SUM(cr.P3Count) > 0 THEN 'Medium'
            WHEN SUM(cr.P4Count) > 0 THEN 'Low'
            WHEN SUM(cr.P5Count) > 0 THEN 'Clear'
            ELSE NULL
        END,
        SUM(CASE WHEN cr.RiskRatio >= @RemoveRatio THEN 1 ELSE 0 END),
        SUM(CASE WHEN cr.ReviewState IN ('PendingReview', 'NeedsReReview') THEN 1 ELSE 0 END),
        CAST(MAX(CASE WHEN cb.IrfId IS NULL THEN 1 ELSE 0 END) AS BIT),
        SUM(cr.P0Count), SUM(cr.P1Count), SUM(cr.P2Count),
        SUM(cr.P3Count), SUM(cr.P4Count), SUM(cr.P5Count)
    FROM #ChannelReview cr
    LEFT JOIN #CoveringBackfill cb ON cb.IrfId = cr.IrfId
    GROUP BY cr.UserId
    -- Default filter: enough volume (@MinVolume completed, as the per-channel InsufficientData rule)
    -- AND (pooled ratio >= @MinRiskRatio OR any P0). "Show all" passes @MinRiskRatio = NULL and lifts both.
    HAVING (
            @MinRiskRatio IS NULL
         OR (
                SUM(cr.Completed) >= @MinVolume
            AND (
                    CAST(CASE WHEN SUM(cr.Completed) = 0 THEN 0
                              ELSE CAST(SUM(cr.Bad) AS DECIMAL(18,4)) / NULLIF(SUM(cr.Completed), 0)
                         END AS DECIMAL(10,4)) >= @MinRiskRatio
                 OR SUM(cr.P0Count) > 0
                )
            )
          )
       AND (
            @PendingReviewOnly = 0
         OR SUM(CASE WHEN cr.ReviewState IN ('PendingReview', 'NeedsReReview') THEN 1 ELSE 0 END) > 0
          );

    -- "Needs a call" (Delta 6): keep only interviewers with no connected call in the window — a call logged to
    -- the interviewer's email, answered, lasting at least QCActivitySetting.MinCallSeconds (default 120).
    IF @NeedsCall = 1 AND @IdsOnly = 0
    BEGIN
        DECLARE @MinCallSeconds INT = 120;
        IF OBJECT_ID(N'dbo.QCActivitySetting', N'U') IS NOT NULL
            EXEC sp_executesql N'
                SELECT @MinCall = ISNULL(MAX(CASE WHEN Name = ''MinCallSeconds'' THEN TRY_CAST(Value AS INT) END), @MinCall)
                FROM dbo.QCActivitySetting',
                N'@MinCall INT OUTPUT', @MinCall = @MinCallSeconds OUTPUT;

        DELETE f
        FROM #Filtered f
        INNER JOIN dbo.UserList ul ON ul.id = f.InterviewerId
        WHERE EXISTS (
            SELECT 1 FROM dbo.CallRecordingData c
            WHERE c.CallRecipient = ul.EmailId
              AND c.DateCreated >= @FromUtc
              AND c.DateCreated <  @ToUtc
              AND ISNULL(TRY_CAST(NULLIF(LTRIM(RTRIM(c.Duration)), '') AS INT), 0) >= @MinCallSeconds
              AND ISNULL(c.[Status], '') NOT LIKE '%busy%'
              AND ISNULL(c.[Status], '') NOT LIKE '%no-answer%'
              AND ISNULL(c.[Status], '') NOT LIKE '%NO_ANSWER%'
              AND ISNULL(c.[Status], '') NOT LIKE '%fail%'
              AND ISNULL(c.[Status], '') NOT LIKE '%cancel%');
    END

    DECLARE @TotalRows INT = (SELECT COUNT(1) FROM #Filtered);

    IF @IdsOnly = 1
    BEGIN
        SELECT
            f.InterviewerId,
            MAX(ISNULL(ip.NickName, '')) AS CodedId,
            MAX(LTRIM(RTRIM(CONCAT(ISNULL(ul.FirstName, ''), N' ', ISNULL(ul.LastName, ''))))) AS InterviewerName,
            MAX(ISNULL(ul.EmailId, '')) AS InterviewerEmail,
            MAX(f.OverallRiskRatio) AS OverallRiskRatio,
            MAX(f.WorstSeverity) AS WorstSeverity,
            MAX(f.PriorityTier) AS PriorityTier,
            MAX(f.TotalCompleted) AS TotalCompleted,
            MAX(f.TotalBad) AS TotalBad
        FROM #Filtered f
        INNER JOIN dbo.UserList ul ON ul.id = f.InterviewerId
        LEFT JOIN dbo.InterviewerProfile ip ON ip.UserId = f.InterviewerId
        GROUP BY f.InterviewerId;

        DROP TABLE IF EXISTS #Filtered;
        DROP TABLE IF EXISTS #CoveringBackfill;
        DROP TABLE IF EXISTS #ChannelReview;
        DROP TABLE IF EXISTS #Mapped;
        DROP TABLE IF EXISTS #Completed;
        DROP TABLE IF EXISTS #ActiveTi;
        DROP TABLE IF EXISTS #ScopedIrfs;
        RETURN;
    END

    -- Page of rows (same joins and sort as before, so paging is unchanged). The extra
    -- profile / activity / lifetime / AI stats below are computed for these rows only.
    CREATE TABLE #Page (
        SortOrder     BIGINT NOT NULL PRIMARY KEY,
        InterviewerId INT NOT NULL,
        ProfileId     INT NULL
    );

    -- Default sort (decision D1): Impact first (two P0s outrank 50% on four interviews), then the earlier
    -- order. @SortBy = 'Ratio' keeps the earlier order (worst severity, ratio).
    INSERT INTO #Page (SortOrder, InterviewerId, ProfileId)
    SELECT
        ROW_NUMBER() OVER (ORDER BY
            CASE WHEN @SortBy = 'Impact' THEN f.Impact ELSE 0 END DESC,
            CASE f.WorstSeverity
                WHEN 'P0' THEN 0 WHEN 'P1' THEN 1 WHEN 'P2' THEN 2
                WHEN 'P3' THEN 3 WHEN 'P4' THEN 4 WHEN 'P5' THEN 5 ELSE 9
            END ASC,
            ISNULL(f.OverallRiskRatio, 0) DESC,
            f.HighRiskChannels DESC,
            f.InterviewerId,
            ip.Id),
        f.InterviewerId,
        ip.Id
    FROM #Filtered f
    INNER JOIN dbo.UserList ul ON ul.id = f.InterviewerId
    LEFT JOIN dbo.InterviewerProfile ip ON ip.UserId = f.InterviewerId
    ORDER BY
        CASE WHEN @SortBy = 'Impact' THEN f.Impact ELSE 0 END DESC,
        CASE f.WorstSeverity
            WHEN 'P0' THEN 0 WHEN 'P1' THEN 1 WHEN 'P2' THEN 2
            WHEN 'P3' THEN 3 WHEN 'P4' THEN 4 WHEN 'P5' THEN 5 ELSE 9
        END ASC,
        ISNULL(f.OverallRiskRatio, 0) DESC,
        f.HighRiskChannels DESC,
        f.InterviewerId,
        ip.Id
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
    OPTION (RECOMPILE);

    -- AI penalties applied automatically: QcInterviewAIMergeJob writes PaymentSLADetails with
    -- CreatedBy = 'AISystem'; AutoapprovePaymentSLA approves it. One row per completed interview in
    -- scope and window that has an AISystem row with a deduction above 0.
    --   IsHumanReviewed: ModifiedBy is a person, or a human PaymentSLADetailsHistory row exists
    --   (the auto-approve timer overwrites ModifiedBy, so the history row is needed).
    -- Set-based (Fix 4A): one grouped pass over PaymentSLADetails joined to the window, then one semi-join pass
    -- over PaymentSLADetailsHistory, instead of two lookups per completed interview. MoneyProtectionWallet is
    -- no longer read (the amount was removed from this page).
    CREATE TABLE #AiPen (
        InterviewId     INT NOT NULL PRIMARY KEY,
        DeductionPct    INT NOT NULL,
        IsApproved      BIT NOT NULL,
        IsHumanReviewed BIT NOT NULL
    );

    INSERT INTO #AiPen (InterviewId, DeductionPct, IsApproved, IsHumanReviewed)
    SELECT
        p.Interviewid,
        MAX(ISNULL(p.PaymentDeduction, 0)),
        CAST(MAX(CASE WHEN ISNULL(p.IsApproved, 0) = 1 THEN 1 ELSE 0 END) AS BIT),
        CAST(MAX(CASE WHEN NULLIF(LTRIM(RTRIM(p.ModifiedBy)), '') IS NOT NULL
                       AND LTRIM(RTRIM(p.ModifiedBy)) NOT IN ('AISystem', 'System') THEN 1 ELSE 0 END) AS BIT)
    FROM #Completed c
    INNER JOIN dbo.PaymentSLADetails p ON p.Interviewid = c.InterviewId
    WHERE LTRIM(RTRIM(ISNULL(p.CreatedBy, ''))) = 'AISystem'
    GROUP BY p.Interviewid
    HAVING MAX(ISNULL(p.PaymentDeduction, 0)) > 0
    OPTION (RECOMPILE);

    UPDATE ap SET IsHumanReviewed = 1
    FROM #AiPen ap
    WHERE ap.IsHumanReviewed = 0
      AND EXISTS (
            SELECT 1 FROM dbo.PaymentSLADetailsHistory h
            WHERE h.Interviewid = ap.InterviewId
              AND NULLIF(LTRIM(RTRIM(h.CreatedBy)), '') IS NOT NULL
              AND LTRIM(RTRIM(h.CreatedBy)) NOT IN ('AISystem', 'System'))
    OPTION (RECOMPILE);

    -- Result set 1
    SELECT
        f.InterviewerId,
        ISNULL(ip.NickName, '') AS CodedId,
        LTRIM(RTRIM(CONCAT(ISNULL(ul.FirstName, ''), N' ', ISNULL(ul.LastName, '')))) AS InterviewerName,
        ISNULL(ul.EmailId, '') AS InterviewerEmail,
        CAST(ul.MobileNumber AS VARCHAR(30)) AS MobileNumber,
        f.ChannelsActive,
        f.TotalCompleted,
        f.TotalBad,
        f.OverallRiskRatio,
        f.WorstSeverity,
        f.PriorityTier,
        f.HighRiskChannels,
        f.PendingReviewChannels,
        f.CoverageIncomplete,
        f.P0Count, f.P1Count, f.P2Count, f.P3Count, f.P4Count, f.P5Count,
        @TotalRows AS TotalRows,
        -- Profile
        NULLIF(LTRIM(RTRIM(ip.ResumeLink)), '') AS ResumeLink,
        COALESCE(NULLIF(LTRIM(RTRIM(ip.CompanyName)), ''), NULLIF(LTRIM(RTRIM(ip.EmploymentComment)), '')) AS CompanyName,
        CAST(ISNULL(ip.IsEmployed, 0) AS BIT) AS IsEmployed,
        CAST(CASE WHEN NULLIF(LTRIM(RTRIM(ip.VideoLink)), '') IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS HasVideoKyc,
        NULLIF(LTRIM(RTRIM(ip.VideoLink)), '') AS VideoLink,
        CAST(ISNULL(ip.IsVideoApproved, 0) AS BIT) AS IsVideoApproved,
        NULLIF(LTRIM(RTRIM(ip.AcknowledgementStatus)), '') AS AcknowledgementStatus,
        ip.Isavailable AS IsAvailable,
        ul.LastLogin,
        CAST(ISNULL(ip.IsHoldPayment, 0) AS BIT) AS IsHoldPayment,
        CAST(ISNULL(ul.IsInterviewerAccess, 0) AS BIT) AS HasPortalAccess,
        -- Activity
        lt.LastInterviewAt,
        ISNULL(fut.FutureInterviews, 0) AS FutureInterviews,
        CAST(CASE WHEN EXISTS (
                SELECT 1 FROM dbo.SuspiciousInterviewer si
                WHERE si.UserId = ul.id AND si.SuspiciousStatus = 1) THEN 1 ELSE 0 END AS BIT) AS ActiveSuspicious,
        -- Lifetime (same definitions as usp_GetInterviewerInsights with no IRF)
        ISNULL(lt.LifetimeInterviews, 0) AS LifetimeInterviews,
        ISNULL(lt.LifetimeCompleted, 0) AS LifetimeCompleted,
        ISNULL(lt.LifetimeSelected, 0) AS LifetimeSelected,
        ISNULL(lt.LifetimeRejected, 0) AS LifetimeRejected,
        ISNULL(lt.InterviewerNoShows, 0) AS InterviewerNoShows,
        ISNULL(lt.CandidateNoShows, 0) AS CandidateNoShows,
        ISNULL(lt.RedoRequests, 0) AS RedoRequests,
        ISNULL(lt.AiTranscriptFlagged, 0) AS AiTranscriptFlagged,
        ISNULL(lj.LateJoins, 0) AS LateJoins,
        ISNULL(pen.Penalties, 0) AS Penalties,
        ISNULL(rel.ReliabilityScore, 0) AS ReliabilityScore,
        -- AI penalties, window (this interviewer's interviews on their active channels)
        ISNULL(ai.AiPenalised, 0) AS AiPenalised,
        ISNULL(ai.AiAutoApproved, 0) AS AiAutoApproved,
        ISNULL(ai.AiHumanReviewed, 0) AS AiHumanReviewed,
        ai.AiAvgPct,
        -- QC Tracking (appended): High-Risk decision on the interviewer (latest RedMarkerStatusDetails Type HighRisk)
        hrs.[Status] AS HighRiskStatus,
        hrs.CreatedBy AS HighRiskStatusBy,
        hrs.CreatedOn AS HighRiskStatusOn,
        hrs.Comment AS HighRiskStatusComment,
        CAST(ISNULL(ul.IsActive, 1) AS BIT) AS InterviewerIsActive,
        -- Impact (bad x severity weight), Removed-from (in-scope channels), Trend (previous window of the same length)
        f.Impact,
        ISNULL(chs.ChannelsTotal, 0) AS ChannelsTotal,
        ISNULL(chs.ChannelsRemoved, 0) AS ChannelsRemoved,
        ISNULL(prv.PrevCompleted, 0) AS PrevCompleted,
        ISNULL(prv.PrevBad, 0) AS PrevBad
    FROM #Page pg
    INNER JOIN #Filtered f ON f.InterviewerId = pg.InterviewerId
    INNER JOIN dbo.UserList ul ON ul.id = f.InterviewerId
    LEFT JOIN dbo.InterviewerProfile ip ON ip.Id = pg.ProfileId
    OUTER APPLY (
        SELECT
            COUNT(DISTINCT im.id) AS LifetimeInterviews,
            COUNT(DISTINCT CASE WHEN ic.MeetingStatus IN (4, 6, 7, 8) THEN im.id END) AS LifetimeCompleted,
            COUNT(DISTINCT CASE WHEN ic.MeetingStatus IN (4, 6, 7, 8) AND ic.InterviewStatus IN (2, 4) THEN im.id END) AS LifetimeSelected,
            COUNT(DISTINCT CASE WHEN ic.MeetingStatus IN (4, 6, 7, 8) AND ic.InterviewStatus IN (1, 3) THEN im.id END) AS LifetimeRejected,
            COUNT(DISTINCT CASE WHEN ic.MeetingStatus IN (5, 9) THEN im.id END) AS InterviewerNoShows,
            COUNT(DISTINCT CASE WHEN ic.MeetingStatus = 1 THEN im.id END) AS CandidateNoShows,
            COUNT(DISTINCT CASE WHEN im.RedoInterview = 1 THEN im.id END) AS RedoRequests,
            COUNT(DISTINCT CASE WHEN ic.AITranscriptVerdict = 'yes' THEN im.id END) AS AiTranscriptFlagged,
            MAX(CASE WHEN ic.MeetingStatus IN (4, 6, 7, 8) THEN im.interviewDate END) AS LastInterviewAt
        FROM dbo.InterviewMeetings im
        INNER JOIN dbo.InterviewComments ic ON ic.InterviewId = im.id
        WHERE im.confirmationStatus = 1
          AND im.interviewer = ul.EmailId
    ) lt
    OUTER APPLY (
        SELECT COUNT(im.id) AS FutureInterviews
        FROM dbo.InterviewMeetings im
        WHERE im.interviewer = ul.EmailId
          AND im.interviewDate > DATEADD(MINUTE, 330, GETDATE())
          AND ISNULL(im.IsDeleted, 0) = 0
          AND ISNULL(im.isAutoCancelled, 0) = 0
          AND CAST(im.confirmationStatus AS VARCHAR(20)) NOT IN ('2', 'Cancelled')
    ) fut
    OUTER APPLY (
        SELECT COUNT(DISTINCT im.id) AS LateJoins
        FROM dbo.InterviewMeetingsJoinTime imj
        INNER JOIN dbo.InterviewMeetings im ON im.id = imj.interviewid
        WHERE DATEDIFF(MINUTE, im.interviewDate, imj.Joinedon) > 5
          AND im.interviewer = ul.EmailId
    ) lj
    OUTER APPLY (
        SELECT COUNT(DISTINCT ipt.id) AS Penalties
        FROM dbo.InterviewPoints ipt
        INNER JOIN dbo.InterviewMeetings im ON im.id = ipt.Interviewid
        WHERE (ipt.PenaltyAmount > 0 OR ipt.Comment LIKE '%Late Report%')
          AND im.interviewer = ul.EmailId
    ) pen
    OUTER APPLY (
        SELECT SUM(trs.Point) AS ReliabilityScore
        FROM dbo.TransInterviewersReliabilityScores trs
        INNER JOIN dbo.InterviewerProfile ip2 ON ip2.id = trs.InterviewerId
        INNER JOIN dbo.UserList ul2 ON ul2.id = ip2.UserId
        WHERE ul2.EmailId = ul.EmailId
    ) rel
    OUTER APPLY (
        SELECT
            COUNT(1) AS AiPenalised,
            SUM(CASE WHEN ap.IsApproved = 1 AND ap.IsHumanReviewed = 0 THEN 1 ELSE 0 END) AS AiAutoApproved,
            SUM(CASE WHEN ap.IsHumanReviewed = 1 THEN 1 ELSE 0 END) AS AiHumanReviewed,
            CAST(AVG(CAST(ap.DeductionPct AS DECIMAL(10,2))) AS DECIMAL(10,2)) AS AiAvgPct
        FROM (SELECT DISTINCT m.InterviewId FROM #Mapped m WHERE m.UserId = pg.InterviewerId) mi
        INNER JOIN #AiPen ap ON ap.InterviewId = mi.InterviewId
    ) ai
    OUTER APPLY (
        SELECT TOP 1 r.[Status], r.CreatedBy, r.CreatedOn, r.Comment
        FROM dbo.RedMarkerStatusDetails r
        WHERE r.InterviewerUserid = f.InterviewerId AND r.[Type] = 'HighRisk'
        ORDER BY r.CreatedOn DESC, r.id DESC
    ) hrs
    OUTER APPLY (
        -- "Removed from 1 of 3 channels": in-scope channels the interviewer is on, and inactive on.
        SELECT COUNT(DISTINCT ti.IRFId) AS ChannelsTotal,
               COUNT(DISTINCT CASE WHEN ISNULL(ti.IsActive, 0) = 0 THEN ti.IRFId END) AS ChannelsRemoved
        FROM dbo.TransInterviewer ti
        INNER JOIN #ScopedIrfs s ON s.IrfId = ti.IRFId
        WHERE ti.UserId = f.InterviewerId
    ) chs
    OUTER APPLY (
        -- Trend: completed and bad (P0-P5) interviews in the previous window of the same length, on in-scope
        -- channels, by the interviewer's email or coded id.
        SELECT COUNT(DISTINCT im.id) AS PrevCompleted,
               COUNT(DISTINCT CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) LIKE 'P[0-5]' THEN im.id END) AS PrevBad
        FROM dbo.InterviewMeetings im
        INNER JOIN dbo.InterviewComments ic ON ic.InterviewId = im.id
        INNER JOIN #ScopedIrfs s ON s.IrfName = im.IRFId
        LEFT JOIN dbo.RiskCategory rc ON rc.InterviewId = im.id
        WHERE ic.MeetingStatus IN (4, 6, 7, 8)
          AND im.interviewDate >= @PrevFromUtc
          AND im.interviewDate <  @FromUtc
          AND im.interviewer IN (ul.EmailId, NULLIF(LTRIM(RTRIM(ip.NickName)), ''))
    ) prv
    ORDER BY pg.SortOrder
    OPTION (RECOMPILE);

    -- Deduction review requests (phase 2), per window: received on in-scope channels with the
    -- request date in the window; upheld = Reduced + Refunded among decided (Expired is not a decision).
    DECLARE @RevReceived INT = 0, @RevDecided INT = 0, @RevUpheld INT = 0, @RevRefunded DECIMAL(18,2) = NULL;
    IF OBJECT_ID(N'dbo.DeductionReviewRequest', N'U') IS NOT NULL
        EXEC sp_executesql N'
            SELECT
                @Received = COUNT(1),
                @Decided  = SUM(CASE WHEN r.Status IN (''Stands'', ''Reduced'', ''Refunded'') THEN 1 ELSE 0 END),
                @Upheld   = SUM(CASE WHEN r.Status IN (''Reduced'', ''Refunded'') THEN 1 ELSE 0 END),
                @Refunded = SUM(CASE WHEN r.Status IN (''Reduced'', ''Refunded'') THEN r.RefundedAmount END)
            FROM dbo.DeductionReviewRequest r
            INNER JOIN #ScopedIrfs si ON si.IrfId = r.IrfId
            WHERE CAST(DATEADD(MINUTE, 330, r.RequestedAtUtc) AS DATE) >= @FromIst
              AND CAST(DATEADD(MINUTE, 330, r.RequestedAtUtc) AS DATE) <= @ToIst;',
            N'@FromIst DATE, @ToIst DATE, @Received INT OUTPUT, @Decided INT OUTPUT, @Upheld INT OUTPUT, @Refunded DECIMAL(18,2) OUTPUT',
            @FromIst = @FromIst, @ToIst = @ToIst, @Received = @RevReceived OUTPUT, @Decided = @RevDecided OUTPUT,
            @Upheld = @RevUpheld OUTPUT, @Refunded = @RevRefunded OUTPUT;

    -- Result set 2: summary from same filtered set; AI totals cover every completed interview
    -- in scope and window (all interviewers, not only the listed ones).
    SELECT
        @TotalRows AS InterviewersAboveThreshold,
        ISNULL((
            SELECT COUNT(DISTINCT cr.IrfId)
            FROM #ChannelReview cr
            INNER JOIN #Filtered f ON f.InterviewerId = cr.UserId
        ), 0) AS ChannelsAffected,
        ISNULL((
            SELECT COUNT(1) FROM #Filtered WHERE PendingReviewChannels > 0
        ), 0) AS PendingReviewInterviewers,
        (SELECT COUNT(1) FROM #AiPen) AS AiPenalisedReports,
        (SELECT COUNT(1) FROM #AiPen WHERE IsApproved = 1 AND IsHumanReviewed = 0) AS AiAutoApprovedReports,
        (SELECT COUNT(1) FROM #AiPen WHERE IsHumanReviewed = 1) AS AiHumanReviewedReports,
        (SELECT CAST(AVG(CAST(DeductionPct AS DECIMAL(10,2))) AS DECIMAL(10,2)) FROM #AiPen) AS AiAvgPct,
        ISNULL(@RevReceived, 0) AS ReviewRequestsReceived,
        ISNULL(@RevDecided, 0) AS ReviewRequestsDecided,
        ISNULL(@RevUpheld, 0) AS ReviewRequestsUpheld,
        CAST(CASE WHEN ISNULL(@RevDecided, 0) = 0 THEN NULL
                  ELSE ISNULL(@RevUpheld, 0) * 100.0 / @RevDecided END AS DECIMAL(10,2)) AS ReviewUpheldPct,
        @RevRefunded AS ReviewRefundedAmount;

    -- Result set 3: upheld rate per AI check for the same requests (in-scope channels, request date in the window).
    -- Upheld = Reduced + Refunded; Expired is not a decision. Empty when the review table is not deployed.
    IF OBJECT_ID(N'dbo.DeductionReviewRequest', N'U') IS NOT NULL
        EXEC sp_executesql N'
            SELECT
                r.AiCheck,
                COUNT(1) AS Requests,
                SUM(CASE WHEN r.Status IN (''Stands'', ''Reduced'', ''Refunded'') THEN 1 ELSE 0 END) AS Decided,
                SUM(CASE WHEN r.Status IN (''Reduced'', ''Refunded'') THEN 1 ELSE 0 END) AS Upheld,
                SUM(CASE WHEN r.Status = ''Stands'' THEN 1 ELSE 0 END) AS Stands,
                SUM(CASE WHEN r.Status = ''Open'' THEN 1 ELSE 0 END) AS [Open],
                CAST(ISNULL(MAX(CAST(pol.ExcludeFromAutoApprove AS INT)), 0) AS BIT) AS ExcludeFromAutoApprove
            FROM dbo.DeductionReviewRequest r
            INNER JOIN #ScopedIrfs si ON si.IrfId = r.IrfId
            LEFT JOIN dbo.DeductionReviewCheckPolicy pol ON pol.AiCheck = r.AiCheck
            WHERE CAST(DATEADD(MINUTE, 330, r.RequestedAtUtc) AS DATE) >= @FromIst
              AND CAST(DATEADD(MINUTE, 330, r.RequestedAtUtc) AS DATE) <= @ToIst
            GROUP BY r.AiCheck
            ORDER BY SUM(CASE WHEN r.Status IN (''Stands'', ''Reduced'', ''Refunded'') THEN 1 ELSE 0 END) DESC, r.AiCheck;',
            N'@FromIst DATE, @ToIst DATE', @FromIst = @FromIst, @ToIst = @ToIst;
    ELSE
        SELECT CAST(NULL AS VARCHAR(30)) AS AiCheck, 0 AS Requests, 0 AS Decided, 0 AS Upheld, 0 AS Stands, 0 AS [Open],
               CAST(0 AS BIT) AS ExcludeFromAutoApprove
        WHERE 1 = 0;

    DROP TABLE IF EXISTS #AiPen;
    DROP TABLE IF EXISTS #Page;
    DROP TABLE IF EXISTS #Filtered;
    DROP TABLE IF EXISTS #CoveringBackfill;
    DROP TABLE IF EXISTS #ChannelReview;
    DROP TABLE IF EXISTS #Mapped;
    DROP TABLE IF EXISTS #Completed;
    DROP TABLE IF EXISTS #ActiveTi;
    DROP TABLE IF EXISTS #ScopedIrfs;
END
GO
