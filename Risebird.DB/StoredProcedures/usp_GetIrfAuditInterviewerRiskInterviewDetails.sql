CREATE OR ALTER PROCEDURE [dbo].[usp_GetIrfAuditInterviewerRiskInterviewDetails]
    @IrfId               INT,
    @FromIst             DATE = NULL,
    @ToIst               DATE = NULL,
    @TransInterviewerId  INT = NULL  -- optional: one interviewer only
AS
BEGIN
    SET NOCOUNT ON;

    IF @IrfId IS NULL OR @IrfId <= 0
    BEGIN
        SELECT
            CAST(0 AS INT) AS TransInterviewerId,
            CAST(0 AS INT) AS InterviewId,
            CAST(NULL AS DATETIME) AS InterviewDate,
            CAST('' AS VARCHAR(200)) AS InterviewerRaw,
            CAST('' AS VARCHAR(4)) AS Severity,
            CAST('' AS VARCHAR(MAX)) AS Name,
            CAST('' AS VARCHAR(500)) AS Description,
            CAST('' AS VARCHAR(500)) AS SeverityBreakdown
        WHERE 1 = 0;
        RETURN;
    END

    IF OBJECT_ID(N'dbo.RiskCategory', N'U') IS NULL
    BEGIN
        RAISERROR('dbo.RiskCategory is missing. Deploy RiskCategory.sql first.', 16, 1);
        RETURN;
    END

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
            CAST(0 AS INT) AS TransInterviewerId,
            CAST(0 AS INT) AS InterviewId,
            CAST(NULL AS DATETIME) AS InterviewDate,
            CAST('' AS VARCHAR(200)) AS InterviewerRaw,
            CAST('' AS VARCHAR(4)) AS Severity,
            CAST('' AS VARCHAR(MAX)) AS Name,
            CAST('' AS VARCHAR(500)) AS Description,
            CAST('' AS VARCHAR(500)) AS SeverityBreakdown
        WHERE 1 = 0;
        RETURN;
    END

    CREATE TABLE #Active (
        TransInterviewerId INT NOT NULL PRIMARY KEY,
        UserId             INT NULL,
        InterviewerEmail   VARCHAR(200) NULL,
        InterviewerCodedId VARCHAR(50) NULL
    );

    INSERT INTO #Active (TransInterviewerId, UserId, InterviewerEmail, InterviewerCodedId)
    SELECT
        ti.id,
        COALESCE(ti.UserId, iv.UserId, ipr.UserId),
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
      AND ISNULL(ti.IsActive, 0) = 1
      AND (@TransInterviewerId IS NULL OR ti.id = @TransInterviewerId);

    CREATE TABLE #Completed (
        InterviewId    INT NOT NULL PRIMARY KEY,
        InterviewerRaw VARCHAR(200) NOT NULL,
        InterviewDate  DATETIME NULL
    );

    INSERT INTO #Completed (InterviewId, InterviewerRaw, InterviewDate)
    SELECT
        im.id,
        LTRIM(RTRIM(im.interviewer)),
        im.interviewDate
    FROM dbo.InterviewMeetings im
    INNER JOIN dbo.InterviewComments ic ON ic.InterviewId = im.id
    WHERE im.IRFId = @IrfName
      AND ic.MeetingStatus IN (4, 6, 7, 8)
      AND CAST(DATEADD(MINUTE, 330, im.interviewDate) AS DATE) >= @FromIst
      AND CAST(DATEADD(MINUTE, 330, im.interviewDate) AS DATE) <= @ToIst
      AND NULLIF(LTRIM(RTRIM(im.interviewer)), '') IS NOT NULL;

    CREATE TABLE #Mapped (
        TransInterviewerId INT NOT NULL,
        InterviewId        INT NOT NULL,
        PRIMARY KEY (TransInterviewerId, InterviewId)
    );

    INSERT INTO #Mapped (TransInterviewerId, InterviewId)
    SELECT DISTINCT a.TransInterviewerId, c.InterviewId
    FROM #Completed c
    INNER JOIN dbo.UserList ul ON ul.EmailId = c.InterviewerRaw
    INNER JOIN #Active a ON a.UserId = ul.id
    WHERE a.UserId IS NOT NULL;

    INSERT INTO #Mapped (TransInterviewerId, InterviewId)
    SELECT DISTINCT a.TransInterviewerId, c.InterviewId
    FROM #Completed c
    INNER JOIN dbo.UserList ul ON LTRIM(RTRIM(ul.EmailId)) = c.InterviewerRaw
    INNER JOIN #Active a ON a.UserId = ul.id
    WHERE a.UserId IS NOT NULL
      AND NOT EXISTS (
            SELECT 1 FROM #Mapped m
            WHERE m.TransInterviewerId = a.TransInterviewerId AND m.InterviewId = c.InterviewId
      );

    INSERT INTO #Mapped (TransInterviewerId, InterviewId)
    SELECT DISTINCT a.TransInterviewerId, c.InterviewId
    FROM #Completed c
    INNER JOIN #Active a ON a.InterviewerEmail = c.InterviewerRaw
    WHERE NOT EXISTS (
        SELECT 1 FROM #Mapped m
        WHERE m.TransInterviewerId = a.TransInterviewerId AND m.InterviewId = c.InterviewId
    );

    INSERT INTO #Mapped (TransInterviewerId, InterviewId)
    SELECT DISTINCT a.TransInterviewerId, c.InterviewId
    FROM #Completed c
    INNER JOIN #Active a ON a.InterviewerCodedId = c.InterviewerRaw
    WHERE NOT EXISTS (
        SELECT 1 FROM #Mapped m
        WHERE m.TransInterviewerId = a.TransInterviewerId AND m.InterviewId = c.InterviewId
    );

    INSERT INTO #Mapped (TransInterviewerId, InterviewId)
    SELECT DISTINCT a.TransInterviewerId, c.InterviewId
    FROM #Completed c
    INNER JOIN dbo.InterviewerProfile ip ON LTRIM(RTRIM(ip.NickName)) = c.InterviewerRaw
    INNER JOIN #Active a ON a.UserId = ip.UserId
    WHERE a.UserId IS NOT NULL
      AND NOT EXISTS (
            SELECT 1 FROM #Mapped m
            WHERE m.TransInterviewerId = a.TransInterviewerId AND m.InterviewId = c.InterviewId
      );

    -- Only interviews that have a RiskCategory severity (so P-level rows have Description/Breakdown)
    SELECT
        m.TransInterviewerId,
        c.InterviewId,
        c.InterviewDate,
        c.InterviewerRaw,
        UPPER(LTRIM(RTRIM(rc.Severity))) AS Severity,
        rc.Name,
        rc.Description,
        rc.SeverityBreakdown
    FROM #Mapped m
    INNER JOIN #Completed c ON c.InterviewId = m.InterviewId
    INNER JOIN dbo.RiskCategory rc ON rc.InterviewId = m.InterviewId
    WHERE NULLIF(LTRIM(RTRIM(ISNULL(rc.Severity, ''))), '') IS NOT NULL
      AND rc.Severity LIKE 'P[0-5]'
    ORDER BY
        m.TransInterviewerId,
        TRY_CAST(SUBSTRING(UPPER(LTRIM(RTRIM(rc.Severity))), 2, 1) AS INT) ASC,
        c.InterviewDate DESC,
        c.InterviewId DESC;

    DROP TABLE IF EXISTS #Mapped;
    DROP TABLE IF EXISTS #Completed;
    DROP TABLE IF EXISTS #Active;
END
GO
