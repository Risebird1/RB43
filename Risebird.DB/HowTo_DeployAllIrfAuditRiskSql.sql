/* =============================================================================
   IRF Audit Risk — ALL changed SQL (run on target DB in order)
   Generated for deploy. Source files under Risebird.DB/
   ============================================================================= */
SET NOCOUNT ON;
GO

PRINT '--- 1) RiskCategory table + Severity columns ---';
GO
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'RiskCategory')
BEGIN

CREATE TABLE [dbo].[RiskCategory](
	[Id]          [int] IDENTITY(1,1) NOT NULL,
	[InterviewId] [int] NOT NULL,
	[Name]        [varchar](max) NULL,
	[Description] [varchar](500) NULL,
	[Severity]    [varchar](4) NULL,
	[SeverityBreakdown] [varchar](500) NULL,
	[CreatedOn]   [datetime] NOT NULL CONSTRAINT [DF_RiskCategory_CreatedOn] DEFAULT (GETDATE()),
	[ModifiedOn]  [datetime] NULL,
 CONSTRAINT [PK_RiskCategory] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_RiskCategory_InterviewId] UNIQUE NONCLUSTERED ([InterviewId] ASC)
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

END
ELSE
BEGIN

IF NOT EXISTS(
    SELECT * FROM sys.columns
    WHERE Name = N'Description'
      AND Object_ID = OBJECT_ID(N'RiskCategory'))
BEGIN
    ALTER TABLE RiskCategory ADD Description varchar(500) NULL
END

IF NOT EXISTS(
    SELECT * FROM sys.columns
    WHERE Name = N'ModifiedOn'
      AND Object_ID = OBJECT_ID(N'RiskCategory'))
BEGIN
    ALTER TABLE RiskCategory ADD ModifiedOn datetime NULL
END

IF NOT EXISTS(
    SELECT * FROM sys.columns
    WHERE Name = N'Severity'
      AND Object_ID = OBJECT_ID(N'RiskCategory'))
BEGIN
    ALTER TABLE RiskCategory ADD Severity VARCHAR(4) NULL
END

IF NOT EXISTS(
    SELECT * FROM sys.columns
    WHERE Name = N'SeverityBreakdown'
      AND Object_ID = OBJECT_ID(N'RiskCategory'))
BEGIN
    ALTER TABLE RiskCategory ADD SeverityBreakdown VARCHAR(500) NULL
END

END
GO
GO

PRINT '--- 2) usp_AddUpdateRiskCategory (P-levels, no 30d filter) ---';
GO
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddUpdateRiskCategory'))
BEGIN
    DROP PROCEDURE dbo.usp_AddUpdateRiskCategory
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Grades a single interview (@InterviewId) into RiskCategory with P0–P5 severity.
-- Date-windowing is the caller's job (audit candidates / list views), not this producer.
CREATE PROCEDURE [dbo].[usp_AddUpdateRiskCategory]
    @InterviewId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @DeficitMoreThan10_Min INT = 10
    DECLARE @Deficit_P2_Max INT = 20
    DECLARE @Deficit_P1_Max INT = 30
    DECLARE @TranscriptLt4_P2_Min INT = 3
    DECLARE @TranscriptLt4_P1_Min INT = 2
    DECLARE @RatingLt4_P2_Min INT = 3
    DECLARE @RatingLt4_P1_Min INT = 2
    DECLARE @SelectDeficit_Ratio_P3_Min DECIMAL(10,4) = 0.5
    DECLARE @SelectDeficit_Ratio_P2_Min DECIMAL(10,4) = 0.333333
    DECLARE @SelectDeficit_Ratio_P1_Max DECIMAL(10,4) = 0.333333
    DECLARE @RejectDeficit_Ratio_P4_Min DECIMAL(10,4) = 0.333333
    DECLARE @RejectDeficit_Ratio_P3_Min DECIMAL(10,4) = 0.222222
    DECLARE @RejectDeficit_Ratio_P2_Max DECIMAL(10,4) = 0.222222
    DECLARE @ReportScoreLt60_P2_Max INT = 60
    DECLARE @ReportScoreLt60_P1_Max INT = 40
    DECLARE @ReportScoreLt60_P0_Max INT = 20
    DECLARE @TranscriptLt6_P4_Min INT = 5
    DECLARE @TranscriptLt6_P3_Min INT = 4
    DECLARE @TranscriptLt6_P2_Min INT = 3
    DECLARE @TranscriptLt6_P1_Min INT = 2
    DECLARE @Contradiction_MaxScore INT = 5

    DECLARE @Name               VARCHAR(MAX)
    DECLARE @Description        VARCHAR(MAX)
    DECLARE @Severity           VARCHAR(4)
    DECLARE @SeverityBreakdown  VARCHAR(500)
    DECLARE @MinPLevel          INT

    SELECT
        @Name              = STRING_AGG(x.Riskcategory, ','),
        @Description       = STRING_AGG(x.Type, ','),
        @SeverityBreakdown = STRING_AGG(x.Type + ':P' + CAST(x.PLevel AS VARCHAR(1)), ','),
        @MinPLevel         = MIN(x.PLevel)
    FROM (
        -- #1 deficitmorethan10
        SELECT im.id AS InterviewId, 'deficitmorethan10' AS Type, 'High Severity' AS Riskcategory,
               CASE WHEN (pt.DurationSelectedMin - ic.VideoDuration) > @Deficit_P1_Max THEN 0
                    WHEN (pt.DurationSelectedMin - ic.VideoDuration) > @Deficit_P2_Max THEN 1 ELSE 2 END AS PLevel
        FROM InterviewMeetings im
        JOIN InterviewComments ic ON ic.InterviewId = im.id
        JOIN IRF ON IRF.IRFId = im.IRFId
        JOIN PlanTypeChargesMaster pt ON pt.CountryType = IRF.JobCountry
             AND pt.PlanTypeEnumValue = IRF.Heuristicsscore AND IRF.CompanyType = pt.CompanyType
        WHERE im.id = @InterviewId AND ic.MeetingStatus = 4
          AND (ic.VideoDuration - pt.DurationSelectedMin) > (-pt.DurationSelectedMin)
          AND (ic.VideoDuration - pt.DurationSelectedMin) < -@DeficitMoreThan10_Min
        UNION ALL
        -- #2 AIVerdictLessthan4
        SELECT im.id, 'AIVerdictLessthan4', 'High Severity',
               CASE WHEN NULLIF(ic.AITranscriptAnalysisScore,0) < @TranscriptLt4_P1_Min THEN 0
                    WHEN NULLIF(ic.AITranscriptAnalysisScore,0) < @TranscriptLt4_P2_Min THEN 1 ELSE 2 END
        FROM InterviewMeetings im JOIN InterviewComments ic ON ic.InterviewId = im.id
        JOIN IRF ON IRF.IRFId = im.IRFId JOIN Client cl ON cl.id = IRF.ClientId
        WHERE im.id = @InterviewId AND ic.MeetingStatus IN (4,6,7,8)
          AND NULLIF(ic.AITranscriptAnalysisScore,0) < 4
        UNION ALL
        -- #3 CandidateRating
        SELECT im.id, 'CandidateRating', 'High Severity',
               CASE WHEN cfd.Rating < @RatingLt4_P1_Min THEN 0
                    WHEN cfd.Rating < @RatingLt4_P2_Min THEN 1 ELSE 2 END
        FROM InterviewMeetings im JOIN InterviewComments ic ON ic.InterviewId = im.id
        JOIN IRF ON IRF.IRFId = im.IRFId JOIN Client cl ON cl.id = IRF.ClientId
        JOIN CandidateFeedbackDetails cfd ON cfd.InterviewId = im.id
        WHERE im.id = @InterviewId AND ic.MeetingStatus IN (4,6,7,8)
          AND cfd.Rating < 4
        UNION ALL
        -- #4 Selectdeficit
        SELECT im.id, 'Selectdeficit', 'Under Review',
               CASE WHEN CAST(ic.VideoDuration AS DECIMAL(18,4))/NULLIF(pt.DurationSelectedMin,0) < @SelectDeficit_Ratio_P1_Max THEN 1
                    WHEN CAST(ic.VideoDuration AS DECIMAL(18,4))/NULLIF(pt.DurationSelectedMin,0) < @SelectDeficit_Ratio_P3_Min THEN 2 ELSE 3 END
        FROM InterviewMeetings im JOIN InterviewComments ic ON ic.InterviewId = im.id
        JOIN IRF ON IRF.IRFId = im.IRFId
        JOIN PlanTypeChargesMaster pt ON pt.CountryType = IRF.JobCountry
             AND pt.PlanTypeEnumValue = IRF.Heuristicsscore AND IRF.CompanyType = pt.CompanyType
        WHERE im.id = @InterviewId AND ic.MeetingStatus IN (4,6,7,8)
          AND pt.DurationSelectedMin > 0
          AND (pt.DurationSelectedMin * 2 / 3 > ic.VideoDuration) AND ic.InterviewStatus IN (2,4)
        UNION ALL
        -- #5 Rejectdeficit
        SELECT im.id, 'Rejectdeficit', 'Under Review',
               CASE WHEN CAST(ic.VideoDuration AS DECIMAL(18,4))/NULLIF(pt.DurationSelectedMin,0) < @RejectDeficit_Ratio_P2_Max THEN 2
                    WHEN CAST(ic.VideoDuration AS DECIMAL(18,4))/NULLIF(pt.DurationSelectedMin,0) < @RejectDeficit_Ratio_P4_Min THEN 3 ELSE 4 END
        FROM InterviewMeetings im JOIN InterviewComments ic ON ic.InterviewId = im.id
        JOIN IRF ON IRF.IRFId = im.IRFId
        JOIN PlanTypeChargesMaster pt ON pt.CountryType = IRF.JobCountry
             AND pt.PlanTypeEnumValue = IRF.Heuristicsscore AND IRF.CompanyType = pt.CompanyType
        WHERE im.id = @InterviewId AND ic.MeetingStatus IN (4,6,7,8)
          AND pt.DurationSelectedMin > 0
          AND (pt.DurationSelectedMin * 4 / 9 > ic.VideoDuration) AND ic.InterviewStatus IN (1,3,5)
        UNION ALL
        -- #6 AIRedo
        SELECT qca.InterviewId, 'AIRedo', 'Redo Requested', 0
        FROM QcInterviewAIevaluation qca
        WHERE qca.InterviewId = @InterviewId AND ISNULL(qca.AIMergeRedoInterview,0) = 1
        UNION ALL
        -- #7 Report AI Score
        SELECT InterviewId, 'Report AI Score', 'High Severity',
               CASE WHEN NULLIF(AIReportQCAnalysisScore,0) <= @ReportScoreLt60_P0_Max THEN 0
                    WHEN NULLIF(AIReportQCAnalysisScore,0) <= @ReportScoreLt60_P1_Max THEN 1 ELSE 2 END
        FROM InterviewComments
        WHERE InterviewId = @InterviewId AND NULLIF(AIReportQCAnalysisScore,0) < @ReportScoreLt60_P2_Max
        UNION ALL
        -- #8 Transcript AI Score
        SELECT InterviewId, 'Transcript AI Score', 'High Severity',
               CASE WHEN NULLIF(AITranscriptAnalysisScore,0) < @TranscriptLt6_P1_Min THEN 0
                    WHEN NULLIF(AITranscriptAnalysisScore,0) < @TranscriptLt6_P2_Min THEN 1
                    WHEN NULLIF(AITranscriptAnalysisScore,0) < @TranscriptLt6_P3_Min THEN 2
                    WHEN NULLIF(AITranscriptAnalysisScore,0) < @TranscriptLt6_P4_Min THEN 3 ELSE 4 END
        FROM InterviewComments
        WHERE InterviewId = @InterviewId AND NULLIF(AITranscriptAnalysisScore,0) < 6
        UNION ALL
        -- #9 Contradiction
        SELECT qca.InterviewId, 'Contradiction AI Score', 'High Severity',
               CASE CAST(TRY_CAST(qca.ContradictionScore AS DECIMAL(10,2)) AS INT)
                    WHEN 1 THEN 0 WHEN 2 THEN 1 WHEN 3 THEN 2 WHEN 4 THEN 3 WHEN 5 THEN 4 END
        FROM QcInterviewAIevaluation qca
        WHERE qca.InterviewId = @InterviewId
          AND TRY_CAST(qca.ContradictionScore AS DECIMAL(10,2)) BETWEEN 1 AND @Contradiction_MaxScore
        UNION ALL
        -- #10 No-show
        SELECT im.id,
               CASE WHEN imj.Joinedon IS NULL AND imc.PerformedOn IS NULL THEN 'No Show'
                    WHEN imj.Joinedon IS NULL THEN 'Interviewer No Show'
                    ELSE 'Candidate No Show' END,
               'Fake Report', 0
        FROM InterviewMeetings im JOIN InterviewComments ic ON ic.InterviewId = im.id
        OUTER APPLY (SELECT MAX(Joinedon) AS Joinedon FROM InterviewMeetingsJoinTime WHERE Interviewid = im.id) imj
        OUTER APPLY (SELECT MAX(PerformedOn) AS PerformedOn FROM PageClickDetails
                     WHERE ButtonAction='JoinMeeting' AND Interviewid=im.id AND ProfileType='Candidate') imc
        WHERE im.id = @InterviewId
          AND im.confirmationStatus = 1
          AND im.ZoomMeetingId IN (SELECT DISTINCT Meetingid FROM ZoomMeetingTransmission)
          AND (imj.Joinedon IS NULL OR imc.PerformedOn IS NULL)
          AND ic.MeetingStatus NOT IN (1,2,3,5,9)
    ) x
    WHERE x.InterviewId = @InterviewId AND x.PLevel IS NOT NULL

    IF @MinPLevel IS NOT NULL
        SET @Severity = 'P' + CAST(@MinPLevel AS VARCHAR(1))

    IF EXISTS (SELECT 1 FROM RiskCategory WHERE InterviewId = @InterviewId)
        UPDATE RiskCategory SET Name=@Name, Description=@Description,
               Severity=@Severity, SeverityBreakdown=@SeverityBreakdown, ModifiedOn=GETDATE()
        WHERE InterviewId = @InterviewId
    ELSE IF ISNULL(LTRIM(RTRIM(@Description)), '') <> ''
        INSERT INTO RiskCategory (InterviewId, Name, Description, Severity, SeverityBreakdown, CreatedOn)
        VALUES (@InterviewId, @Name, @Description, @Severity, @SeverityBreakdown, GETDATE())
END
GO
GO

PRINT '--- 3) usp_GetRiskCategoryByInterviewId (+ Severity) ---';
GO
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetRiskCategoryByInterviewId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRiskCategoryByInterviewId
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- =====================================================================
-- Procedure : usp_GetRiskCategoryByInterviewId
-- Purpose   : Returns the RiskCategory row for the given InterviewId,
--             joined with basic interview info from InterviewMeetings.
-- Usage     : EXEC usp_GetRiskCategoryByInterviewId @InterviewId = 1234
-- =====================================================================
CREATE PROCEDURE [dbo].[usp_GetRiskCategoryByInterviewId]
    @InterviewId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        rc.Id,
        rc.InterviewId,
        rc.Name,
        rc.Description,
        rc.Severity,
        rc.SeverityBreakdown,
        rc.CreatedOn,
        rc.ModifiedOn,
        im.candidateName,
        im.candidateEmail,
        im.Interviewer,
        im.interviewDate,
        im.IRFId,
        IRF.PrimarySkillInvite AS PrimarySkill
    FROM RiskCategory rc
    LEFT JOIN InterviewMeetings im ON im.id  = rc.InterviewId
    LEFT JOIN IRF               ON IRF.IRFId = im.IRFId
    WHERE rc.InterviewId = @InterviewId

END
GO
GO

PRINT '--- 4) usp_GetRiskCategories (+ Severity) ---';
GO
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetRiskCategories'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRiskCategories
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- =====================================================================
-- Procedure : usp_GetRiskCategories
-- Purpose   : Returns all RiskCategory rows joined with InterviewMeetings
--             filtered by interviewDate between @StartDate and @EndDate.
-- Usage     : EXEC usp_GetRiskCategories @StartDate = '2026-06-01', @EndDate = '2026-07-01'
-- =====================================================================
CREATE PROCEDURE [dbo].[usp_GetRiskCategories]
    @StartDate DATETIME,
    @EndDate   DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        rc.Id,
        rc.InterviewId,
        rc.Name,
        rc.Description,
        rc.Severity,
        rc.SeverityBreakdown,
        rc.CreatedOn,
        rc.ModifiedOn,
        im.candidateName,
        im.candidateEmail,
        im.Interviewer,
        im.interviewDate,
        im.IRFId,
        IRF.PrimarySkillInvite AS PrimarySkill
    FROM RiskCategory rc
    INNER JOIN InterviewMeetings im ON im.id   = rc.InterviewId
    LEFT  JOIN IRF               ON IRF.IRFId  = im.IRFId
    WHERE im.interviewDate >= @StartDate
      AND im.interviewDate <  @EndDate
    ORDER BY im.interviewDate DESC

END
GO
GO

PRINT '--- 5) usp_GetRiskCategoryBackfillCandidatesByIrf (NEW) ---';
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_GetRiskCategoryBackfillCandidatesByIrf]
    @IrfId   INT,
    @FromIst DATE = NULL,
    @ToIst   DATE = NULL,
    @MaxRows INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    IF @IrfId IS NULL OR @IrfId <= 0
    BEGIN
        SELECT CAST(0 AS INT) AS InterviewId WHERE 1 = 0;
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

    IF @MaxRows IS NULL OR @MaxRows <= 0
        SET @MaxRows = 100;
    IF @MaxRows > 500
        SET @MaxRows = 500;

    -- Only interviews that still need a RiskCategory / Severity row.
    SELECT TOP (@MaxRows)
        im.id AS InterviewId
    FROM dbo.InterviewMeetings im
    INNER JOIN dbo.IRF irf ON irf.IRFId = im.IRFId
    INNER JOIN dbo.InterviewComments ic ON ic.InterviewId = im.id
    LEFT JOIN dbo.RiskCategory rc ON rc.InterviewId = im.id
    WHERE irf.id = @IrfId
      AND ic.MeetingStatus IN (4, 6, 7, 8)
      AND CAST(DATEADD(MINUTE, 330, im.interviewDate) AS DATE) >= @FromIst
      AND CAST(DATEADD(MINUTE, 330, im.interviewDate) AS DATE) <= @ToIst
      AND (
            rc.InterviewId IS NULL
         OR NULLIF(LTRIM(RTRIM(ISNULL(rc.Severity, ''))), '') IS NULL
          )
    ORDER BY im.interviewDate DESC, im.id DESC;
END
GO
GO

PRINT '--- 6) usp_GetIrfAuditInterviewerRiskBreakdown (NEW, optimized) ---';
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_GetIrfAuditInterviewerRiskBreakdown]
    @IrfId   INT,
    @FromIst DATE = NULL,
    @ToIst   DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Tunable recommendation thresholds
    DECLARE @MinVolume        INT = 3;
    DECLARE @RemoveRatio      DECIMAL(10,4) = 0.50;
    DECLARE @HighSevRatio_Rem DECIMAL(10,4) = 0.25;
    DECLARE @WatchRatio       DECIMAL(10,4) = 0.25;

    IF @IrfId IS NULL OR @IrfId <= 0
    BEGIN
        SELECT
            CAST(0 AS INT) AS TransInterviewerId,
            CAST(0 AS INT) AS InterviewerId,
            CAST('' AS VARCHAR(50)) AS InterviewerCodedId,
            CAST('' AS VARCHAR(200)) AS InterviewerEmail,
            CAST(0 AS INT) AS IrfId,
            CAST('' AS NVARCHAR(200)) AS IrfName,
            CAST(0 AS INT) AS TotalCompleted,
            CAST(0 AS INT) AS RiskInterviews,
            CAST(0 AS INT) AS P0Count, CAST(0 AS INT) AS P1Count, CAST(0 AS INT) AS P2Count,
            CAST(0 AS INT) AS P3Count, CAST(0 AS INT) AS P4Count, CAST(0 AS INT) AS P5Count,
            CAST(NULL AS DECIMAL(10,4)) AS RiskRatio,
            CAST(NULL AS DECIMAL(10,4)) AS HighSevRatio,
            CAST(NULL AS INT) AS WorstSeverityInterviewId,
            CAST('' AS VARCHAR(20)) AS Recommendation
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
            CAST(0 AS INT) AS TransInterviewerId, CAST(0 AS INT) AS InterviewerId,
            CAST('' AS VARCHAR(50)) AS InterviewerCodedId, CAST('' AS VARCHAR(200)) AS InterviewerEmail,
            CAST(0 AS INT) AS IrfId, CAST('' AS NVARCHAR(200)) AS IrfName,
            CAST(0 AS INT) AS TotalCompleted, CAST(0 AS INT) AS RiskInterviews,
            CAST(0 AS INT) AS P0Count, CAST(0 AS INT) AS P1Count, CAST(0 AS INT) AS P2Count,
            CAST(0 AS INT) AS P3Count, CAST(0 AS INT) AS P4Count, CAST(0 AS INT) AS P5Count,
            CAST(NULL AS DECIMAL(10,4)) AS RiskRatio, CAST(NULL AS DECIMAL(10,4)) AS HighSevRatio,
            CAST(NULL AS INT) AS WorstSeverityInterviewId, CAST('' AS VARCHAR(20)) AS Recommendation
        WHERE 1 = 0;
        RETURN;
    END

    /* ---- Active interviewers on this IRF (small set) ---- */
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

    /* ---- Completed interviews in window (use IRF varchar key on InterviewMeetings) ---- */
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

    /* ---- Map interview → active channel (equality joins only; no per-row CROSS APPLY) ---- */
    CREATE TABLE #Mapped (
        TransInterviewerId INT NOT NULL,
        InterviewId        INT NOT NULL,
        PRIMARY KEY (TransInterviewerId, InterviewId)
    );

    -- 1) Interview.email → UserList → channel.UserId
    INSERT INTO #Mapped (TransInterviewerId, InterviewId)
    SELECT DISTINCT a.TransInterviewerId, c.InterviewId
    FROM #Completed c
    INNER JOIN dbo.UserList ul ON ul.EmailId = c.InterviewerRaw
    INNER JOIN #Active a ON a.UserId = ul.id
    WHERE a.UserId IS NOT NULL;

    -- 1b) Trimmed email fallback for mismatched whitespace in UserList
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

    -- 2) Direct email match to channel email
    INSERT INTO #Mapped (TransInterviewerId, InterviewId)
    SELECT DISTINCT a.TransInterviewerId, c.InterviewId
    FROM #Completed c
    INNER JOIN #Active a ON a.InterviewerEmail = c.InterviewerRaw
    WHERE NOT EXISTS (
        SELECT 1 FROM #Mapped m
        WHERE m.TransInterviewerId = a.TransInterviewerId AND m.InterviewId = c.InterviewId
    );

    -- 3) NickName / coded-id match
    INSERT INTO #Mapped (TransInterviewerId, InterviewId)
    SELECT DISTINCT a.TransInterviewerId, c.InterviewId
    FROM #Completed c
    INNER JOIN #Active a ON a.InterviewerCodedId = c.InterviewerRaw
    WHERE NOT EXISTS (
        SELECT 1 FROM #Mapped m
        WHERE m.TransInterviewerId = a.TransInterviewerId AND m.InterviewId = c.InterviewId
    );

    -- 4) Remaining: nickname on InterviewerProfile → UserId → channel
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

    /* ---- Aggregate with RiskCategory ---- */
    ;WITH InterviewAgg AS (
        SELECT
            m.TransInterviewerId,
            COUNT(DISTINCT m.InterviewId) AS TotalCompleted,
            COUNT(DISTINCT CASE WHEN rc.InterviewId IS NOT NULL THEN m.InterviewId END) AS RiskInterviews,
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
    )
    SELECT
        a.TransInterviewerId,
        ISNULL(a.InterviewerId, 0) AS InterviewerId,
        ISNULL(a.InterviewerCodedId, '') AS InterviewerCodedId,
        ISNULL(a.InterviewerEmail, '') AS InterviewerEmail,
        a.IrfId,
        ISNULL(@IrfName, '') AS IrfName,
        ISNULL(ia.TotalCompleted, 0) AS TotalCompleted,
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
        END AS Recommendation
    FROM #Active a
    LEFT JOIN InterviewAgg ia ON ia.TransInterviewerId = a.TransInterviewerId
    LEFT JOIN WorstInterview wi ON wi.TransInterviewerId = a.TransInterviewerId
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

    DROP TABLE IF EXISTS #Mapped;
    DROP TABLE IF EXISTS #Completed;
    DROP TABLE IF EXISTS #Active;
END
GO
GO

PRINT '--- 7) usp_GetAutoQcRemovalMonitorData (+ RiskAuditRemoval) ---';
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_GetAutoQcRemovalMonitorData]
    @FromUtc DATETIME = NULL,
    @ToUtc DATETIME = NULL,
    @IrfId INT = NULL,
    @AuditSource VARCHAR(50) = NULL,
    @Verdict VARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'dbo.TransInterviewerAuditLog', N'U') IS NULL
        RETURN;

    IF @FromUtc IS NULL
        SET @FromUtc = DATEADD(DAY, -30, GETUTCDATE());
    IF @ToUtc IS NULL
        SET @ToUtc = GETUTCDATE();

    ;WITH Removals AS (
        SELECT
            al.Id AS AuditLogId,
            al.ChangedAt AS RemovedAt,
            ip.NickName AS InterviewerCodedId,
            ti.IRFId AS IrfId,
            irf.IRFId AS IrfName,
            al.TriggeringInterviewId,
            al.ToValue AS Verdict,
            al.AuditSource AS Source,
            al.BackfillBatchId,
            al.ExtraMetadata,
            al.TransInterviewerId
        FROM dbo.TransInterviewerAuditLog al
        INNER JOIN dbo.TransInterviewer ti ON ti.id = al.TransInterviewerId
        LEFT JOIN dbo.InterviewerProfile ip ON ip.UserId = ti.UserId
        LEFT JOIN dbo.IRF irf ON irf.id = ti.IRFId
        WHERE al.EventType IN ('AutoQcRemoval', 'RiskAuditRemoval')
          AND al.ChangedAt >= @FromUtc
          AND al.ChangedAt <= @ToUtc
          AND (@IrfId IS NULL OR ti.IRFId = @IrfId)
          AND (@AuditSource IS NULL OR al.AuditSource = @AuditSource)
          AND (@Verdict IS NULL OR al.ToValue = @Verdict)
    ),
    Reactivations AS (
        SELECT al.TransInterviewerId, MAX(al.ChangedAt) AS ReactivatedAt
        FROM dbo.TransInterviewerAuditLog al
        WHERE al.EventType = 'RbVetFlippedToYes'
        GROUP BY al.TransInterviewerId
    )
    SELECT
        r.AuditLogId,
        r.RemovedAt,
        r.InterviewerCodedId,
        r.IrfId,
        r.IrfName,
        r.TriggeringInterviewId,
        r.Verdict,
        r.Source,
        r.BackfillBatchId,
        r.ExtraMetadata,
        CAST(CASE WHEN react.ReactivatedAt IS NOT NULL AND react.ReactivatedAt > r.RemovedAt THEN 1 ELSE 0 END AS BIT) AS WasReactivated
    FROM Removals r
    LEFT JOIN Reactivations react ON react.TransInterviewerId = r.TransInterviewerId
    ORDER BY r.RemovedAt DESC, r.AuditLogId DESC;

    SELECT
        ti.IRFId AS IrfId,
        irf.IRFId AS IrfName,
        COUNT(1) AS Removals,
        SUM(CASE WHEN react.ReactivatedAt IS NOT NULL AND react.ReactivatedAt > al.ChangedAt THEN 1 ELSE 0 END) AS Reactivations,
        SUM(CASE WHEN al.ToValue = 'suspicious' THEN 1 ELSE 0 END) AS SuspiciousRemovals,
        SUM(CASE WHEN al.ToValue = 'redo' THEN 1 ELSE 0 END) AS RedoRemovals
    FROM dbo.TransInterviewerAuditLog al
    INNER JOIN dbo.TransInterviewer ti ON ti.id = al.TransInterviewerId
    LEFT JOIN dbo.IRF irf ON irf.id = ti.IRFId
    OUTER APPLY (
        SELECT MAX(x.ChangedAt) AS ReactivatedAt
        FROM dbo.TransInterviewerAuditLog x
        WHERE x.TransInterviewerId = al.TransInterviewerId
          AND x.EventType = 'RbVetFlippedToYes'
    ) react
    WHERE al.EventType IN ('AutoQcRemoval', 'RiskAuditRemoval')
      AND al.ChangedAt >= @FromUtc
      AND al.ChangedAt <= @ToUtc
      AND (@IrfId IS NULL OR ti.IRFId = @IrfId)
      AND (@AuditSource IS NULL OR al.AuditSource = @AuditSource)
      AND (@Verdict IS NULL OR al.ToValue = @Verdict)
    GROUP BY ti.IRFId, irf.IRFId
    ORDER BY Removals DESC, ti.IRFId;
END
GO
GO

PRINT '--- 8) usp_GetTransInterviewerInactiveAuditSummariesByIrfId (+ RiskAuditRemoval) ---';
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_GetTransInterviewerInactiveAuditSummariesByIrfId]
    @IrfId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'dbo.TransInterviewerAuditLog', N'U') IS NULL
        RETURN;

    SELECT
        ti.id AS TransInterviewerId,
        CAST(1 AS BIT) AS InactiveAuditSummaryLoaded,
        latest.InactiveAuditEventType,
        latest.InactiveAuditReasonDetail,
        latest.InactiveAuditTriggeringInterviewId,
        latest.InactiveAuditChangedAt
    FROM dbo.TransInterviewer ti
    OUTER APPLY (
        SELECT TOP 1
            al.EventType AS InactiveAuditEventType,
            CASE
                WHEN al.EventType IN ('AutoQcRemoval', 'RiskAuditRemoval') THEN COALESCE(NULLIF(LTRIM(RTRIM(al.ReasonCategory)), ''), al.ToValue)
                ELSE al.ReasonCategory
            END AS InactiveAuditReasonDetail,
            al.TriggeringInterviewId AS InactiveAuditTriggeringInterviewId,
            al.ChangedAt AS InactiveAuditChangedAt
        FROM dbo.TransInterviewerAuditLog al
        WHERE al.TransInterviewerId = ti.id
          AND al.EventType IN ('RbVetFlippedToNo', 'ClientVetFlippedToNo', 'AutoQcRemoval', 'RiskAuditRemoval')
        ORDER BY al.ChangedAt DESC, al.Id DESC
    ) latest
    WHERE ti.IRFId = @IrfId;
END
GO
GO

PRINT 'ALL IRF AUDIT RISK SQL DEPLOYED.';
GO
