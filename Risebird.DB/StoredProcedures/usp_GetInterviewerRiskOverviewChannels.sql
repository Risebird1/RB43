/*
================================================================================
usp_GetInterviewerRiskOverviewChannels — per-channel drill-down for one
canonical interviewer (UserList.id) on the High-Risk Interviewers overview.

Numbers/recommendation/review-state MUST match usp_GetIrfAuditInterviewerRiskBreakdown
for the same interviewer + IRF + window (same thresholds + mapping rules).

Thresholds MUST stay in sync with usp_GetIrfAuditInterviewerRiskBreakdown and
DmIvAutoQcAuditConstants.IrfAudit* in C#.
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_GetInterviewerRiskOverviewChannels]
    @InterviewerId INT,          -- canonical UserList.id
    @FromIst       DATE = NULL,
    @ToIst         DATE = NULL,
    @OwnerEmail    VARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

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

    IF @InterviewerId IS NULL OR @InterviewerId <= 0
    BEGIN
        SELECT
            CAST(0 AS INT) AS TransInterviewerId, CAST(0 AS INT) AS IrfId, CAST('' AS NVARCHAR(200)) AS IrfName,
            CAST(NULL AS VARCHAR(200)) AS ChannelOwnerEmail, CAST(NULL AS NVARCHAR(300)) AS ChannelOwnerDisplay,
            CAST(0 AS INT) AS Completed, CAST(0 AS INT) AS Bad, CAST(NULL AS DECIMAL(10,4)) AS RiskRatio,
            CAST(0 AS INT) AS P0Count, CAST(0 AS INT) AS P1Count, CAST(0 AS INT) AS P2Count,
            CAST(0 AS INT) AS P3Count, CAST(0 AS INT) AS P4Count, CAST(0 AS INT) AS P5Count,
            CAST('' AS VARCHAR(20)) AS Recommendation, CAST('' AS VARCHAR(20)) AS ReviewState,
            CAST(NULL AS VARCHAR(150)) AS ReviewedBy, CAST(NULL AS DATETIME) AS ReviewedAt,
            CAST(NULL AS INT) AS WorstSeverityInterviewId,
            CAST(NULL AS DATETIME) AS LastBackfillAtUtc,
            CAST(NULL AS DATE) AS LastBackfillFromIst, CAST(NULL AS DATE) AS LastBackfillToIst,
            CAST(1 AS BIT) AS CoverageIncomplete
        WHERE 1 = 0;
        RETURN;
    END

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
        IrfName VARCHAR(200) NOT NULL,
        RisebirdHr INT NULL
    );

    INSERT INTO #ScopedIrfs (IrfId, IrfName, RisebirdHr)
    SELECT irf.id, irf.IRFId, irf.RisebirdHr
    FROM dbo.IRF irf
    LEFT JOIN dbo.UserList ulco ON ulco.id = irf.RisebirdHr
    WHERE NULLIF(LTRIM(RTRIM(irf.IRFId)), '') IS NOT NULL
      AND (
            @OwnerFilterActive = 0
         OR LTRIM(RTRIM(ulco.EmailId)) = LTRIM(RTRIM(@OwnerEmail))
          );

    CREATE TABLE #ActiveTi (
        TransInterviewerId INT NOT NULL PRIMARY KEY,
        IrfId              INT NOT NULL,
        UserId             INT NOT NULL,
        InterviewerEmail   VARCHAR(200) NULL,
        InterviewerCodedId VARCHAR(50) NULL,
        IsActive           BIT NOT NULL
    );

    INSERT INTO #ActiveTi (TransInterviewerId, IrfId, UserId, InterviewerEmail, InterviewerCodedId, IsActive)
    SELECT
        ti.id,
        ti.IRFId,
        COALESCE(ti.UserId, iv.UserId, ipr.UserId),
        NULLIF(LTRIM(RTRIM(COALESCE(ulTi.EmailId, ulIv.EmailId, ulIp.EmailId))), ''),
        NULLIF(LTRIM(RTRIM(COALESCE(ipTi.NickName, ipr.NickName, ipUser.NickName))), ''),
        ISNULL(ti.IsActive, 0)
    FROM dbo.TransInterviewer ti
    INNER JOIN #ScopedIrfs s ON s.IrfId = ti.IRFId
    LEFT JOIN dbo.Interviewer iv ON iv.id = ti.InterviewerId
    LEFT JOIN dbo.InterviewerProfile ipr ON ipr.id = ti.InterviewerId
    LEFT JOIN dbo.UserList ulTi ON ulTi.id = ti.UserId
    LEFT JOIN dbo.UserList ulIv ON ulIv.id = iv.UserId
    LEFT JOIN dbo.UserList ulIp ON ulIp.id = ipr.UserId
    LEFT JOIN dbo.InterviewerProfile ipTi ON ipTi.UserId = ti.UserId
    LEFT JOIN dbo.InterviewerProfile ipUser ON ipUser.UserId = COALESCE(ti.UserId, iv.UserId, ipr.UserId)
    WHERE COALESCE(ti.UserId, iv.UserId, ipr.UserId) = @InterviewerId
      AND (
            ISNULL(ti.IsActive, 0) = 1
         OR EXISTS (
                SELECT 1 FROM dbo.TransInterviewerAuditLog l
                WHERE l.TransInterviewerId = ti.id AND l.EventType = 'RiskAuditRemoval'
            )
          );

    DELETE FROM #ActiveTi WHERE UserId IS NULL;

    CREATE INDEX IX_ActiveTi_Irf ON #ActiveTi(IrfId);
    CREATE INDEX IX_ActiveTi_Email ON #ActiveTi(InterviewerEmail);
    CREATE INDEX IX_ActiveTi_Coded ON #ActiveTi(InterviewerCodedId);

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
    INNER JOIN #ActiveTi a ON a.IrfId = s.IrfId
    WHERE ic.MeetingStatus IN (4, 6, 7, 8)
      AND CAST(DATEADD(MINUTE, 330, im.interviewDate) AS DATE) >= @FromIst
      AND CAST(DATEADD(MINUTE, 330, im.interviewDate) AS DATE) <= @ToIst
      AND NULLIF(LTRIM(RTRIM(im.interviewer)), '') IS NOT NULL
    GROUP BY im.id, s.IrfId, LTRIM(RTRIM(im.interviewer)), im.interviewDate;

    CREATE TABLE #Mapped (
        TransInterviewerId INT NOT NULL,
        IrfId              INT NOT NULL,
        InterviewId        INT NOT NULL,
        PRIMARY KEY (TransInterviewerId, InterviewId)
    );

    INSERT INTO #Mapped (TransInterviewerId, IrfId, InterviewId)
    SELECT DISTINCT a.TransInterviewerId, c.IrfId, c.InterviewId
    FROM #Completed c
    INNER JOIN dbo.UserList ul ON ul.EmailId = c.InterviewerRaw
    INNER JOIN #ActiveTi a ON a.UserId = ul.id AND a.IrfId = c.IrfId;

    INSERT INTO #Mapped (TransInterviewerId, IrfId, InterviewId)
    SELECT DISTINCT a.TransInterviewerId, c.IrfId, c.InterviewId
    FROM #Completed c
    INNER JOIN dbo.UserList ul ON LTRIM(RTRIM(ul.EmailId)) = c.InterviewerRaw
    INNER JOIN #ActiveTi a ON a.UserId = ul.id AND a.IrfId = c.IrfId
    WHERE NOT EXISTS (SELECT 1 FROM #Mapped x WHERE x.TransInterviewerId = a.TransInterviewerId AND x.InterviewId = c.InterviewId);

    INSERT INTO #Mapped (TransInterviewerId, IrfId, InterviewId)
    SELECT DISTINCT a.TransInterviewerId, c.IrfId, c.InterviewId
    FROM #Completed c
    INNER JOIN #ActiveTi a ON a.IrfId = c.IrfId AND a.InterviewerEmail = c.InterviewerRaw
    WHERE NOT EXISTS (SELECT 1 FROM #Mapped x WHERE x.TransInterviewerId = a.TransInterviewerId AND x.InterviewId = c.InterviewId);

    INSERT INTO #Mapped (TransInterviewerId, IrfId, InterviewId)
    SELECT DISTINCT a.TransInterviewerId, c.IrfId, c.InterviewId
    FROM #Completed c
    INNER JOIN #ActiveTi a ON a.IrfId = c.IrfId AND a.InterviewerCodedId = c.InterviewerRaw
    WHERE NOT EXISTS (SELECT 1 FROM #Mapped x WHERE x.TransInterviewerId = a.TransInterviewerId AND x.InterviewId = c.InterviewId);

    INSERT INTO #Mapped (TransInterviewerId, IrfId, InterviewId)
    SELECT DISTINCT a.TransInterviewerId, c.IrfId, c.InterviewId
    FROM #Completed c
    INNER JOIN dbo.InterviewerProfile ip ON LTRIM(RTRIM(ip.NickName)) = c.InterviewerRaw
    INNER JOIN #ActiveTi a ON a.UserId = ip.UserId AND a.IrfId = c.IrfId
    WHERE ip.UserId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM #Mapped x WHERE x.TransInterviewerId = a.TransInterviewerId AND x.InterviewId = c.InterviewId);

    ;WITH InterviewAgg AS (
        SELECT
            m.TransInterviewerId,
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
        GROUP BY m.TransInterviewerId, m.IrfId
    ),
    WorstInterview AS (
        SELECT InterviewId, TransInterviewerId
        FROM (
            SELECT
                m.InterviewId,
                m.TransInterviewerId,
                ROW_NUMBER() OVER (
                    PARTITION BY m.TransInterviewerId
                    ORDER BY
                        CASE UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, ''))))
                            WHEN 'P0' THEN 0 WHEN 'P1' THEN 1 WHEN 'P2' THEN 2
                            WHEN 'P3' THEN 3 WHEN 'P4' THEN 4 WHEN 'P5' THEN 5 ELSE 9
                        END ASC,
                        m.InterviewId DESC
                ) AS rn
            FROM #Mapped m
            INNER JOIN dbo.RiskCategory rc ON rc.InterviewId = m.InterviewId
            WHERE UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) LIKE 'P[0-5]'
        ) x
        WHERE rn = 1
    ),
    LatestReview AS (
        SELECT
            l.TransInterviewerId, l.EventType, l.ChangedBy, l.ChangedAt,
            ROW_NUMBER() OVER (PARTITION BY l.TransInterviewerId ORDER BY l.ChangedAt DESC, l.Id DESC) AS rn
        FROM dbo.TransInterviewerAuditLog l
        INNER JOIN #ActiveTi a ON a.TransInterviewerId = l.TransInterviewerId
        WHERE l.EventType IN ('RiskAuditRemoval', 'RiskAuditKept')
    ),
    ReviewPick AS (
        SELECT TransInterviewerId, EventType, ChangedBy, ChangedAt
        FROM LatestReview WHERE rn = 1
    ),
    BadAfterKeep AS (
        SELECT DISTINCT m.TransInterviewerId
        FROM #Mapped m
        INNER JOIN #Completed c ON c.InterviewId = m.InterviewId
        INNER JOIN dbo.RiskCategory rc ON rc.InterviewId = m.InterviewId
        INNER JOIN ReviewPick rp ON rp.TransInterviewerId = m.TransInterviewerId AND rp.EventType = 'RiskAuditKept'
        WHERE UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) LIKE 'P[0-5]'
          AND c.InterviewDateUtc > rp.ChangedAt
    ),
    LastBackfill AS (
        SELECT IrfId, ExecutedAtUtc, FromIst, ToIst
        FROM (
            SELECT
                r.IrfId, r.ExecutedAtUtc, r.FromIst, r.ToIst,
                ROW_NUMBER() OVER (PARTITION BY r.IrfId ORDER BY r.ExecutedAtUtc DESC) AS rn
            FROM dbo.IrfAuditRiskBackfillRun r
            WHERE @HasRiskBackfillTable = 1
        ) x
        WHERE rn = 1
    )
    SELECT
        a.TransInterviewerId,
        a.IrfId,
        ISNULL(s.IrfName, '') AS IrfName,
        NULLIF(LTRIM(RTRIM(ulco.EmailId)), '') AS ChannelOwnerEmail,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(ulco.EmailId)), '') IS NULL THEN NULL
            WHEN ulco.LastName IS NULL AND NULLIF(LTRIM(RTRIM(ulco.FirstName)), '') IS NOT NULL
                THEN LTRIM(RTRIM(ulco.FirstName)) + N' (' + LTRIM(RTRIM(ulco.EmailId)) + N')'
            WHEN NULLIF(LTRIM(RTRIM(ulco.FirstName)), '') IS NOT NULL OR NULLIF(LTRIM(RTRIM(ulco.LastName)), '') IS NOT NULL
                THEN LTRIM(RTRIM(CONCAT(ISNULL(ulco.FirstName, ''), N' ', ISNULL(ulco.LastName, ''))))
                     + N' (' + LTRIM(RTRIM(ulco.EmailId)) + N')'
            ELSE LTRIM(RTRIM(ulco.EmailId))
        END AS ChannelOwnerDisplay,
        ISNULL(ia.Completed, 0) AS Completed,
        ISNULL(ia.Bad, 0) AS Bad,
        CAST(CASE WHEN ISNULL(ia.Completed, 0) = 0 THEN NULL
                  ELSE CAST(ISNULL(ia.Bad, 0) AS DECIMAL(18,4)) / NULLIF(ia.Completed, 0)
             END AS DECIMAL(10,4)) AS RiskRatio,
        ISNULL(ia.P0Count, 0) AS P0Count,
        ISNULL(ia.P1Count, 0) AS P1Count,
        ISNULL(ia.P2Count, 0) AS P2Count,
        ISNULL(ia.P3Count, 0) AS P3Count,
        ISNULL(ia.P4Count, 0) AS P4Count,
        ISNULL(ia.P5Count, 0) AS P5Count,
        CASE
            WHEN ISNULL(ia.Completed, 0) < @MinVolume THEN 'InsufficientData'
            WHEN (
                    CAST(ISNULL(ia.Bad, 0) AS DECIMAL(18,4)) / NULLIF(ia.Completed, 0) >= @RemoveRatio
                 OR CAST(ISNULL(ia.P0Count, 0) + ISNULL(ia.P1Count, 0) AS DECIMAL(18,4)) / NULLIF(ia.Completed, 0) >= @HighSevRatio_Rem
                 ) THEN 'Remove'
            WHEN (
                    CAST(ISNULL(ia.Bad, 0) AS DECIMAL(18,4)) / NULLIF(ia.Completed, 0) >= @WatchRatio
                 OR ISNULL(ia.P0Count, 0) >= 1
                 ) THEN 'Watch'
            ELSE 'OK'
        END AS Recommendation,
        CASE
            WHEN a.IsActive = 0 AND rp.EventType = 'RiskAuditRemoval' THEN 'Removed'
            WHEN rp.EventType = 'RiskAuditKept' AND bak.TransInterviewerId IS NOT NULL THEN 'NeedsReReview'
            WHEN rp.EventType = 'RiskAuditKept' THEN 'Kept'
            WHEN ISNULL(ia.Completed, 0) >= @MinVolume
                 AND (
                        CAST(ISNULL(ia.Bad, 0) AS DECIMAL(18,4)) / NULLIF(ia.Completed, 0) >= @RemoveRatio
                     OR CAST(ISNULL(ia.P0Count, 0) + ISNULL(ia.P1Count, 0) AS DECIMAL(18,4)) / NULLIF(ia.Completed, 0) >= @HighSevRatio_Rem
                     OR CAST(ISNULL(ia.Bad, 0) AS DECIMAL(18,4)) / NULLIF(ia.Completed, 0) >= @WatchRatio
                     OR ISNULL(ia.P0Count, 0) >= 1
                     )
                THEN 'PendingReview'
            ELSE 'None'
        END AS ReviewState,
        rp.ChangedBy AS ReviewedBy,
        rp.ChangedAt AS ReviewedAt,
        wi.InterviewId AS WorstSeverityInterviewId,
        lb.ExecutedAtUtc AS LastBackfillAtUtc,
        lb.FromIst AS LastBackfillFromIst,
        lb.ToIst AS LastBackfillToIst,
        CAST(CASE
            WHEN lb.IrfId IS NOT NULL AND lb.FromIst <= @FromIst AND lb.ToIst >= @ToIst THEN 0
            ELSE 1
        END AS BIT) AS CoverageIncomplete
    FROM #ActiveTi a
    INNER JOIN #ScopedIrfs s ON s.IrfId = a.IrfId
    LEFT JOIN dbo.UserList ulco ON ulco.id = s.RisebirdHr
    LEFT JOIN InterviewAgg ia ON ia.TransInterviewerId = a.TransInterviewerId
    LEFT JOIN WorstInterview wi ON wi.TransInterviewerId = a.TransInterviewerId
    LEFT JOIN ReviewPick rp ON rp.TransInterviewerId = a.TransInterviewerId
    LEFT JOIN BadAfterKeep bak ON bak.TransInterviewerId = a.TransInterviewerId
    LEFT JOIN LastBackfill lb ON lb.IrfId = a.IrfId
    WHERE ISNULL(ia.Completed, 0) > 0 OR a.IsActive = 1
    ORDER BY
        CASE
            WHEN ISNULL(ia.Completed, 0) < @MinVolume THEN 3
            WHEN (
                    CAST(ISNULL(ia.Bad, 0) AS DECIMAL(18,4)) / NULLIF(ia.Completed, 0) >= @RemoveRatio
                 OR CAST(ISNULL(ia.P0Count, 0) + ISNULL(ia.P1Count, 0) AS DECIMAL(18,4)) / NULLIF(ia.Completed, 0) >= @HighSevRatio_Rem
                 ) THEN 0
            WHEN (
                    CAST(ISNULL(ia.Bad, 0) AS DECIMAL(18,4)) / NULLIF(ia.Completed, 0) >= @WatchRatio
                 OR ISNULL(ia.P0Count, 0) >= 1
                 ) THEN 1
            ELSE 2
        END ASC,
        ISNULL(ia.Bad, 0) * 1.0 / NULLIF(ia.Completed, 0) DESC,
        a.IrfId;

    DROP TABLE IF EXISTS #Mapped;
    DROP TABLE IF EXISTS #Completed;
    DROP TABLE IF EXISTS #ActiveTi;
    DROP TABLE IF EXISTS #ScopedIrfs;
END
GO
