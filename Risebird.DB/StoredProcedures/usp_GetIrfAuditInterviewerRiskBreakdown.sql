CREATE OR ALTER PROCEDURE [dbo].[usp_GetIrfAuditInterviewerRiskBreakdown]
    @IrfId   INT,
    @FromIst DATE = NULL,
    @ToIst   DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Tunable recommendation thresholds (unchanged rule)
    DECLARE @MinVolume        INT = 3;
    DECLARE @RemoveRatio      DECIMAL(10,4) = 0.50;
    DECLARE @HighSevRatio_Rem DECIMAL(10,4) = 0.25;
    DECLARE @WatchRatio       DECIMAL(10,4) = 0.25;

    DECLARE @HasMergedJson BIT = CASE
        WHEN COL_LENGTH('dbo.QcInterviewAIevaluation', 'MergedAIQCDetails') IS NOT NULL THEN 1 ELSE 0 END;
    DECLARE @HasAiMergePenalty BIT = CASE
        WHEN COL_LENGTH('dbo.QcInterviewAIevaluation', 'AIMergeTotalPenalty') IS NOT NULL THEN 1 ELSE 0 END;

    IF @IrfId IS NULL OR @IrfId <= 0
    BEGIN
        SELECT
            CAST(0 AS INT) AS TransInterviewerId,
            CAST(0 AS INT) AS InterviewerId,
            CAST(0 AS INT) AS UserId,
            CAST('' AS VARCHAR(50)) AS InterviewerCodedId,
            CAST('' AS VARCHAR(200)) AS InterviewerEmail,
            CAST(0 AS INT) AS IrfId,
            CAST('' AS NVARCHAR(200)) AS IrfName,
            CAST(0 AS INT) AS TotalCompleted,
            CAST(0 AS INT) AS LifetimeCompleted,
            CAST(0 AS INT) AS RiskInterviews,
            CAST(0 AS INT) AS P0Count, CAST(0 AS INT) AS P1Count, CAST(0 AS INT) AS P2Count,
            CAST(0 AS INT) AS P3Count, CAST(0 AS INT) AS P4Count, CAST(0 AS INT) AS P5Count,
            CAST(NULL AS DECIMAL(10,4)) AS RiskRatio,
            CAST(NULL AS DECIMAL(10,4)) AS HighSevRatio,
            CAST(NULL AS INT) AS WorstSeverityInterviewId,
            CAST('' AS VARCHAR(20)) AS Recommendation,
            CAST(NULL AS VARCHAR(20)) AS QcMergeVerdict,
            CAST(NULL AS INT) AS QcMergeScore,
            CAST(0 AS INT) AS PaymentDeductionTotal,
            CAST('' AS VARCHAR(500)) AS PaymentDeductionReasons
        WHERE 1 = 0;
        RETURN;
    END

    IF OBJECT_ID(N'dbo.RiskCategory', N'U') IS NULL
    BEGIN
        RAISERROR('dbo.RiskCategory is missing. Deploy RiskCategory.sql first.', 16, 1);
        RETURN;
    END

    IF COL_LENGTH('dbo.RiskCategory', 'Severity') IS NULL
    BEGIN
        RAISERROR('dbo.RiskCategory.Severity is missing. Deploy RiskCategory.sql (Severity columns) first.', 16, 1);
        RETURN;
    END

    -- IST window (same normalization as usp_GetIrfAuditDirectory)
    IF @ToIst IS NULL
        SET @ToIst = CAST(DATEADD(MINUTE, 330, GETUTCDATE()) AS DATE);
    IF @FromIst IS NULL
        SET @FromIst = DATEADD(DAY, -30, @ToIst);
    IF @FromIst > @ToIst
        SET @FromIst = DATEADD(DAY, -30, @ToIst);
    IF DATEDIFF(DAY, @FromIst, @ToIst) > 90
        SET @FromIst = DATEADD(DAY, -90, @ToIst);

    DECLARE @IrfName VARCHAR(200);
    SELECT @IrfName = irf.IRFId FROM dbo.IRF irf WHERE irf.id = @IrfId;
    IF @IrfName IS NULL
    BEGIN
        SELECT
            CAST(0 AS INT) AS TransInterviewerId, CAST(0 AS INT) AS InterviewerId, CAST(0 AS INT) AS UserId,
            CAST('' AS VARCHAR(50)) AS InterviewerCodedId, CAST('' AS VARCHAR(200)) AS InterviewerEmail,
            CAST(0 AS INT) AS IrfId, CAST('' AS NVARCHAR(200)) AS IrfName,
            CAST(0 AS INT) AS TotalCompleted, CAST(0 AS INT) AS LifetimeCompleted,
            CAST(0 AS INT) AS RiskInterviews,
            CAST(0 AS INT) AS P0Count, CAST(0 AS INT) AS P1Count, CAST(0 AS INT) AS P2Count,
            CAST(0 AS INT) AS P3Count, CAST(0 AS INT) AS P4Count, CAST(0 AS INT) AS P5Count,
            CAST(NULL AS DECIMAL(10,4)) AS RiskRatio, CAST(NULL AS DECIMAL(10,4)) AS HighSevRatio,
            CAST(NULL AS INT) AS WorstSeverityInterviewId, CAST('' AS VARCHAR(20)) AS Recommendation,
            CAST(NULL AS VARCHAR(20)) AS QcMergeVerdict, CAST(NULL AS INT) AS QcMergeScore,
            CAST(0 AS INT) AS PaymentDeductionTotal, CAST('' AS VARCHAR(500)) AS PaymentDeductionReasons
        WHERE 1 = 0;
        RETURN;
    END

    /* ---- Active interviewers on this IRF ---- */
    CREATE TABLE #Active (
        TransInterviewerId INT NOT NULL PRIMARY KEY,
        InterviewerId      INT NULL,
        UserId             INT NULL,
        IrfId              INT NOT NULL,
        InterviewerEmail   VARCHAR(200) NULL,
        InterviewerCodedId VARCHAR(50) NULL
    );

    INSERT INTO #Active (TransInterviewerId, InterviewerId, UserId, IrfId, InterviewerEmail, InterviewerCodedId)
    SELECT
        ti.id,
        ti.InterviewerId,
        COALESCE(ti.UserId, iv.UserId, ipr.UserId),
        ti.IRFId,
        NULLIF(LTRIM(RTRIM(COALESCE(ulTi.EmailId, ulIv.EmailId, ulIp.EmailId))), ''),
        NULLIF(LTRIM(RTRIM(COALESCE(ipTi.NickName, ipr.NickName, ipUser.NickName))), '')
    FROM dbo.TransInterviewer ti
    LEFT JOIN dbo.Interviewer iv ON iv.id = ti.InterviewerId
    LEFT JOIN dbo.InterviewerProfile ipr ON ipr.id = ti.InterviewerId
    LEFT JOIN dbo.UserList ulTi ON ulTi.id = ti.UserId
    LEFT JOIN dbo.UserList ulIv ON ulIv.id = iv.UserId
    LEFT JOIN dbo.UserList ulIp ON ulIp.id = ipr.UserId
    LEFT JOIN dbo.InterviewerProfile ipTi ON ipTi.UserId = ti.UserId
    LEFT JOIN dbo.InterviewerProfile ipUser ON ipUser.UserId = COALESCE(ti.UserId, iv.UserId, ipr.UserId)
    WHERE ti.IRFId = @IrfId
      AND ISNULL(ti.IsActive, 0) = 1;

    CREATE INDEX IX_Active_UserId ON #Active(UserId);
    CREATE INDEX IX_Active_Email ON #Active(InterviewerEmail);
    CREATE INDEX IX_Active_Coded ON #Active(InterviewerCodedId);

    /* ---- WINDOW completed interviews ---- */
    CREATE TABLE #Completed (
        InterviewId    INT NOT NULL PRIMARY KEY,
        InterviewerRaw VARCHAR(200) NOT NULL
    );

    INSERT INTO #Completed (InterviewId, InterviewerRaw)
    SELECT
        im.id,
        LTRIM(RTRIM(im.interviewer))
    FROM dbo.InterviewMeetings im
    INNER JOIN dbo.InterviewComments ic ON ic.InterviewId = im.id
    WHERE im.IRFId = @IrfName
      AND ic.MeetingStatus IN (4, 6, 7, 8)
      AND CAST(DATEADD(MINUTE, 330, im.interviewDate) AS DATE) >= @FromIst
      AND CAST(DATEADD(MINUTE, 330, im.interviewDate) AS DATE) <= @ToIst
      AND NULLIF(LTRIM(RTRIM(im.interviewer)), '') IS NOT NULL;

    CREATE INDEX IX_Completed_Raw ON #Completed(InterviewerRaw);

    /* ---- LIFETIME completed (no date filter) — display only ---- */
    CREATE TABLE #LifetimeCompleted (
        InterviewId    INT NOT NULL PRIMARY KEY,
        InterviewerRaw VARCHAR(200) NOT NULL
    );

    INSERT INTO #LifetimeCompleted (InterviewId, InterviewerRaw)
    SELECT
        im.id,
        LTRIM(RTRIM(im.interviewer))
    FROM dbo.InterviewMeetings im
    INNER JOIN dbo.InterviewComments ic ON ic.InterviewId = im.id
    WHERE im.IRFId = @IrfName
      AND ic.MeetingStatus IN (4, 6, 7, 8)
      AND NULLIF(LTRIM(RTRIM(im.interviewer)), '') IS NOT NULL;

    CREATE INDEX IX_Life_Raw ON #LifetimeCompleted(InterviewerRaw);

    /* ---- Map helpers ---- */
    CREATE TABLE #Mapped (
        TransInterviewerId INT NOT NULL,
        InterviewId        INT NOT NULL,
        PRIMARY KEY (TransInterviewerId, InterviewId)
    );

    CREATE TABLE #MappedLife (
        TransInterviewerId INT NOT NULL,
        InterviewId        INT NOT NULL,
        PRIMARY KEY (TransInterviewerId, InterviewId)
    );

    CREATE TABLE #MapAll (
        Scope              CHAR(1) NOT NULL,
        TransInterviewerId INT NOT NULL,
        InterviewId        INT NOT NULL,
        PRIMARY KEY (Scope, TransInterviewerId, InterviewId)
    );

    INSERT INTO #MapAll (Scope, TransInterviewerId, InterviewId)
    SELECT DISTINCT s.Scope, a.TransInterviewerId, s.InterviewId
    FROM (
        SELECT 'W' AS Scope, InterviewId, InterviewerRaw FROM #Completed
        UNION ALL
        SELECT 'L', InterviewId, InterviewerRaw FROM #LifetimeCompleted
    ) s
    INNER JOIN dbo.UserList ul ON ul.EmailId = s.InterviewerRaw
    INNER JOIN #Active a ON a.UserId = ul.id
    WHERE a.UserId IS NOT NULL;

    INSERT INTO #MapAll (Scope, TransInterviewerId, InterviewId)
    SELECT DISTINCT s.Scope, a.TransInterviewerId, s.InterviewId
    FROM (
        SELECT 'W' AS Scope, InterviewId, InterviewerRaw FROM #Completed
        UNION ALL
        SELECT 'L', InterviewId, InterviewerRaw FROM #LifetimeCompleted
    ) s
    INNER JOIN dbo.UserList ul ON LTRIM(RTRIM(ul.EmailId)) = s.InterviewerRaw
    INNER JOIN #Active a ON a.UserId = ul.id
    WHERE a.UserId IS NOT NULL
      AND NOT EXISTS (
            SELECT 1 FROM #MapAll x
            WHERE x.Scope = s.Scope AND x.TransInterviewerId = a.TransInterviewerId AND x.InterviewId = s.InterviewId
      );

    INSERT INTO #MapAll (Scope, TransInterviewerId, InterviewId)
    SELECT DISTINCT s.Scope, a.TransInterviewerId, s.InterviewId
    FROM (
        SELECT 'W' AS Scope, InterviewId, InterviewerRaw FROM #Completed
        UNION ALL
        SELECT 'L', InterviewId, InterviewerRaw FROM #LifetimeCompleted
    ) s
    INNER JOIN #Active a ON a.InterviewerEmail = s.InterviewerRaw
    WHERE NOT EXISTS (
        SELECT 1 FROM #MapAll x
        WHERE x.Scope = s.Scope AND x.TransInterviewerId = a.TransInterviewerId AND x.InterviewId = s.InterviewId
    );

    INSERT INTO #MapAll (Scope, TransInterviewerId, InterviewId)
    SELECT DISTINCT s.Scope, a.TransInterviewerId, s.InterviewId
    FROM (
        SELECT 'W' AS Scope, InterviewId, InterviewerRaw FROM #Completed
        UNION ALL
        SELECT 'L', InterviewId, InterviewerRaw FROM #LifetimeCompleted
    ) s
    INNER JOIN #Active a ON a.InterviewerCodedId = s.InterviewerRaw
    WHERE NOT EXISTS (
        SELECT 1 FROM #MapAll x
        WHERE x.Scope = s.Scope AND x.TransInterviewerId = a.TransInterviewerId AND x.InterviewId = s.InterviewId
    );

    INSERT INTO #MapAll (Scope, TransInterviewerId, InterviewId)
    SELECT DISTINCT s.Scope, a.TransInterviewerId, s.InterviewId
    FROM (
        SELECT 'W' AS Scope, InterviewId, InterviewerRaw FROM #Completed
        UNION ALL
        SELECT 'L', InterviewId, InterviewerRaw FROM #LifetimeCompleted
    ) s
    INNER JOIN dbo.InterviewerProfile ip ON LTRIM(RTRIM(ip.NickName)) = s.InterviewerRaw
    INNER JOIN #Active a ON a.UserId = ip.UserId
    WHERE a.UserId IS NOT NULL
      AND NOT EXISTS (
            SELECT 1 FROM #MapAll x
            WHERE x.Scope = s.Scope AND x.TransInterviewerId = a.TransInterviewerId AND x.InterviewId = s.InterviewId
      );

    INSERT INTO #Mapped (TransInterviewerId, InterviewId)
    SELECT TransInterviewerId, InterviewId FROM #MapAll WHERE Scope = 'W';

    INSERT INTO #MappedLife (TransInterviewerId, InterviewId)
    SELECT TransInterviewerId, InterviewId FROM #MapAll WHERE Scope = 'L';

    DROP TABLE IF EXISTS #MapAll;
    /* ---- Window aggregates + lifetime ---- */
    ;WITH InterviewAgg AS (
        SELECT
            m.TransInterviewerId,
            COUNT(DISTINCT m.InterviewId) AS TotalCompleted,
            COUNT(DISTINCT CASE
                WHEN rc.InterviewId IS NOT NULL
                 AND UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) LIKE 'P[0-5]'
                THEN m.InterviewId END) AS RiskInterviews,
            COUNT(DISTINCT CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) = 'P0' THEN m.InterviewId END) AS P0Count,
            COUNT(DISTINCT CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) = 'P1' THEN m.InterviewId END) AS P1Count,
            COUNT(DISTINCT CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) = 'P2' THEN m.InterviewId END) AS P2Count,
            COUNT(DISTINCT CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) = 'P3' THEN m.InterviewId END) AS P3Count,
            COUNT(DISTINCT CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) = 'P4' THEN m.InterviewId END) AS P4Count,
            COUNT(DISTINCT CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) = 'P5' THEN m.InterviewId END) AS P5Count
        FROM #Mapped m
        LEFT JOIN dbo.RiskCategory rc ON rc.InterviewId = m.InterviewId
        GROUP BY m.TransInterviewerId
    ),
    LifeAgg AS (
        SELECT
            ml.TransInterviewerId,
            COUNT(DISTINCT ml.InterviewId) AS LifetimeCompleted
        FROM #MappedLife ml
        GROUP BY ml.TransInterviewerId
    ),
    WorstInterview AS (
        SELECT TransInterviewerId, InterviewId AS WorstSeverityInterviewId
        FROM (
            SELECT
                m.TransInterviewerId,
                m.InterviewId,
                ROW_NUMBER() OVER (
                    PARTITION BY m.TransInterviewerId
                    ORDER BY TRY_CAST(SUBSTRING(UPPER(LTRIM(RTRIM(rc.Severity))), 2, 1) AS INT) ASC,
                             m.InterviewId DESC
                ) AS rn
            FROM #Mapped m
            INNER JOIN dbo.RiskCategory rc ON rc.InterviewId = m.InterviewId
            WHERE NULLIF(LTRIM(RTRIM(ISNULL(rc.Severity, ''))), '') IS NOT NULL
              AND rc.Severity LIKE 'P[0-5]'
        ) x
        WHERE rn = 1
    ),
    -- QC merge verdict/score for worst-severity interview (or latest window interview with QC if no risk)
    QcPick AS (
        SELECT TransInterviewerId, InterviewId
        FROM WorstInterview
        UNION ALL
        SELECT TransInterviewerId, InterviewId
        FROM (
            SELECT
                m.TransInterviewerId,
                m.InterviewId,
                ROW_NUMBER() OVER (PARTITION BY m.TransInterviewerId ORDER BY m.InterviewId DESC) AS rn
            FROM #Mapped m
            WHERE NOT EXISTS (
                SELECT 1 FROM WorstInterview w WHERE w.TransInterviewerId = m.TransInterviewerId
            )
        ) y
        WHERE rn = 1
    ),
    QcMerge AS (
        SELECT
            p.TransInterviewerId,
            CASE
                WHEN ISNULL(q.AIMergeRedoInterview, 0) = 1
                  OR (
                        @HasMergedJson = 1
                    AND ISNULL(TRY_CAST(JSON_VALUE(q.MergedAIQCDetails, '$.RedoInterview') AS BIT), 0) = 1
                     ) THEN 'redo'
                WHEN UPPER(LTRIM(RTRIM(ISNULL(q.AIMergeInterviewerSuspicious, '')))) IN ('YES', 'Y', 'TRUE') THEN 'suspicious'
                WHEN NULLIF(LTRIM(RTRIM(ISNULL(q.qcmergeverdict, ''))), '') IS NOT NULL THEN LTRIM(RTRIM(q.qcmergeverdict))
                ELSE 'ok'
            END AS QcMergeVerdict,
            COALESCE(
                CASE WHEN @HasAiMergePenalty = 1 THEN q.AIMergeTotalPenalty END,
                q.qcmergescore,
                CASE WHEN @HasMergedJson = 1 THEN TRY_CAST(JSON_VALUE(q.MergedAIQCDetails, '$.total_penalty') AS INT) END
            ) AS QcMergeScore
        FROM QcPick p
        LEFT JOIN dbo.QcInterviewAIevaluation q ON q.InterviewId = p.InterviewId
    ),
    PayAgg AS (
        SELECT
            m.TransInterviewerId,
            SUM(ISNULL(ps.PaymentDeduction, 0)) AS PaymentDeductionTotal,
            STUFF(
                CONCAT(
                    CASE WHEN MAX(CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(ps.LimitedHandoncoding,''))), '') IS NOT NULL THEN 1 ELSE 0 END) = 1 THEN ',LimitedHandoncoding' ELSE '' END,
                    CASE WHEN MAX(CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(ps.WrongStatusUpdates,''))), '') IS NOT NULL THEN 1 ELSE 0 END) = 1 THEN ',WrongStatusUpdates' ELSE '' END,
                    CASE WHEN MAX(CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(ps.WeakInfrastructure,''))), '') IS NOT NULL THEN 1 ELSE 0 END) = 1 THEN ',WeakInfrastructure' ELSE '' END,
                    CASE WHEN MAX(CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(ps.ProctoringIgnorance,''))), '') IS NOT NULL THEN 1 ELSE 0 END) = 1 THEN ',ProctoringIgnorance' ELSE '' END,
                    CASE WHEN MAX(CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(ps.UnethicalPractices,''))), '') IS NOT NULL THEN 1 ELSE 0 END) = 1 THEN ',UnethicalPractices' ELSE '' END,
                    CASE WHEN MAX(CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(ps.PoorCommunicationSkills,''))), '') IS NOT NULL THEN 1 ELSE 0 END) = 1 THEN ',PoorCommunicationSkills' ELSE '' END,
                    CASE WHEN MAX(CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(ps.AttendanceJoinCompliance,''))), '') IS NOT NULL THEN 1 ELSE 0 END) = 1 THEN ',AttendanceJoinCompliance' ELSE '' END
                ), 1, 1, '') AS PaymentDeductionReasons
        FROM #Mapped m
        INNER JOIN dbo.PaymentSLADetails ps ON ps.Interviewid = m.InterviewId
        GROUP BY m.TransInterviewerId
    )
    SELECT
        a.TransInterviewerId,
        ISNULL(a.InterviewerId, 0) AS InterviewerId,
        ISNULL(a.UserId, 0) AS UserId,
        ISNULL(a.InterviewerCodedId, '') AS InterviewerCodedId,
        ISNULL(a.InterviewerEmail, '') AS InterviewerEmail,
        a.IrfId,
        ISNULL(@IrfName, '') AS IrfName,
        ISNULL(ia.TotalCompleted, 0) AS TotalCompleted,
        ISNULL(la.LifetimeCompleted, 0) AS LifetimeCompleted,
        ISNULL(ia.RiskInterviews, 0) AS RiskInterviews,
        ISNULL(ia.P0Count, 0) AS P0Count,
        ISNULL(ia.P1Count, 0) AS P1Count,
        ISNULL(ia.P2Count, 0) AS P2Count,
        ISNULL(ia.P3Count, 0) AS P3Count,
        ISNULL(ia.P4Count, 0) AS P4Count,
        ISNULL(ia.P5Count, 0) AS P5Count,
        CAST(CASE WHEN ISNULL(ia.TotalCompleted, 0) = 0 THEN NULL
                  ELSE CAST(ISNULL(ia.RiskInterviews, 0) AS DECIMAL(18,4)) / NULLIF(ia.TotalCompleted, 0)
             END AS DECIMAL(10,4)) AS RiskRatio,
        CAST(CASE WHEN ISNULL(ia.TotalCompleted, 0) = 0 THEN NULL
                  ELSE CAST(ISNULL(ia.P0Count, 0) + ISNULL(ia.P1Count, 0) AS DECIMAL(18,4)) / NULLIF(ia.TotalCompleted, 0)
             END AS DECIMAL(10,4)) AS HighSevRatio,
        wi.WorstSeverityInterviewId,
        CASE
            WHEN ISNULL(ia.TotalCompleted, 0) < @MinVolume THEN 'InsufficientData'
            WHEN (
                    CAST(ISNULL(ia.RiskInterviews, 0) AS DECIMAL(18,4)) / NULLIF(ia.TotalCompleted, 0) >= @RemoveRatio
                 OR CAST(ISNULL(ia.P0Count, 0) + ISNULL(ia.P1Count, 0) AS DECIMAL(18,4)) / NULLIF(ia.TotalCompleted, 0) >= @HighSevRatio_Rem
                 ) THEN 'Remove'
            WHEN (
                    CAST(ISNULL(ia.RiskInterviews, 0) AS DECIMAL(18,4)) / NULLIF(ia.TotalCompleted, 0) >= @WatchRatio
                 OR ISNULL(ia.P0Count, 0) >= 1
                 ) THEN 'Watch'
            ELSE 'OK'
        END AS Recommendation,
        qm.QcMergeVerdict,
        qm.QcMergeScore,
        ISNULL(pa.PaymentDeductionTotal, 0) AS PaymentDeductionTotal,
        ISNULL(pa.PaymentDeductionReasons, '') AS PaymentDeductionReasons
    FROM #Active a
    LEFT JOIN InterviewAgg ia ON ia.TransInterviewerId = a.TransInterviewerId
    LEFT JOIN LifeAgg la ON la.TransInterviewerId = a.TransInterviewerId
    LEFT JOIN WorstInterview wi ON wi.TransInterviewerId = a.TransInterviewerId
    LEFT JOIN QcMerge qm ON qm.TransInterviewerId = a.TransInterviewerId
    LEFT JOIN PayAgg pa ON pa.TransInterviewerId = a.TransInterviewerId
    ORDER BY
        CASE
            WHEN ISNULL(ia.TotalCompleted, 0) < @MinVolume THEN 3
            WHEN (
                    CAST(ISNULL(ia.RiskInterviews, 0) AS DECIMAL(18,4)) / NULLIF(ia.TotalCompleted, 0) >= @RemoveRatio
                 OR CAST(ISNULL(ia.P0Count, 0) + ISNULL(ia.P1Count, 0) AS DECIMAL(18,4)) / NULLIF(ia.TotalCompleted, 0) >= @HighSevRatio_Rem
                 ) THEN 0
            WHEN (
                    CAST(ISNULL(ia.RiskInterviews, 0) AS DECIMAL(18,4)) / NULLIF(ia.TotalCompleted, 0) >= @WatchRatio
                 OR ISNULL(ia.P0Count, 0) >= 1
                 ) THEN 1
            ELSE 2
        END ASC,
        CASE WHEN ISNULL(ia.TotalCompleted, 0) = 0 THEN 0
             ELSE CAST(ISNULL(ia.RiskInterviews, 0) AS DECIMAL(18,4)) / NULLIF(ia.TotalCompleted, 0)
        END DESC,
        ISNULL(ia.P0Count, 0) DESC,
        a.TransInterviewerId;

    DROP TABLE IF EXISTS #MappedLife;
    DROP TABLE IF EXISTS #Mapped;
    DROP TABLE IF EXISTS #LifetimeCompleted;
    DROP TABLE IF EXISTS #Completed;
    DROP TABLE IF EXISTS #Active;
END
GO
