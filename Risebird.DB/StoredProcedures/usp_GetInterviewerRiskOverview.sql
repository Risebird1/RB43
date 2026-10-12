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

Result set 1: page of interviewer rows (+ TotalRows on each row).
Result set 2: summary header (InterviewersAboveThreshold, ChannelsAffected, PendingReviewInterviewers).
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_GetInterviewerRiskOverview]
    @FromIst           DATE = NULL,
    @ToIst             DATE = NULL,
    @OwnerEmail        VARCHAR(200) = NULL,
    @MinRiskRatio      DECIMAL(5,2) = 0.25,  -- NULL = show all (no ratio/P0 default filter)
    @PendingReviewOnly BIT = 0,
    @Offset            INT = 0,
    @PageSize          INT = 25
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
      AND CAST(DATEADD(MINUTE, 330, im.interviewDate) AS DATE) >= @FromIst
      AND CAST(DATEADD(MINUTE, 330, im.interviewDate) AS DATE) <= @ToIst
      AND NULLIF(LTRIM(RTRIM(im.interviewer)), '') IS NOT NULL;

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
        P3Count INT NOT NULL, P4Count INT NOT NULL, P5Count INT NOT NULL
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

    DECLARE @TotalRows INT = (SELECT COUNT(1) FROM #Filtered);

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
        @TotalRows AS TotalRows
    FROM #Filtered f
    INNER JOIN dbo.UserList ul ON ul.id = f.InterviewerId
    LEFT JOIN dbo.InterviewerProfile ip ON ip.UserId = f.InterviewerId
    ORDER BY
        CASE f.WorstSeverity
            WHEN 'P0' THEN 0 WHEN 'P1' THEN 1 WHEN 'P2' THEN 2
            WHEN 'P3' THEN 3 WHEN 'P4' THEN 4 WHEN 'P5' THEN 5 ELSE 9
        END ASC,
        ISNULL(f.OverallRiskRatio, 0) DESC,
        f.HighRiskChannels DESC,
        f.InterviewerId
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

    -- Result set 2: summary from same filtered set
    SELECT
        @TotalRows AS InterviewersAboveThreshold,
        ISNULL((
            SELECT COUNT(DISTINCT cr.IrfId)
            FROM #ChannelReview cr
            INNER JOIN #Filtered f ON f.InterviewerId = cr.UserId
        ), 0) AS ChannelsAffected,
        ISNULL((
            SELECT COUNT(1) FROM #Filtered WHERE PendingReviewChannels > 0
        ), 0) AS PendingReviewInterviewers;

    DROP TABLE IF EXISTS #Filtered;
    DROP TABLE IF EXISTS #CoveringBackfill;
    DROP TABLE IF EXISTS #ChannelReview;
    DROP TABLE IF EXISTS #Mapped;
    DROP TABLE IF EXISTS #Completed;
    DROP TABLE IF EXISTS #ActiveTi;
    DROP TABLE IF EXISTS #ScopedIrfs;
END
GO
