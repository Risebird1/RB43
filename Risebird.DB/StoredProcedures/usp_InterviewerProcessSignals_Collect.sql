/*
================================================================================
usp_InterviewerProcessSignals_Collect — PLI / PQI process signals per completed
interview in a window (QC Tracking, Delta 5). Single source of the rules used by
usp_GetInterviewerProcessSignals, usp_GetHighRiskReports,
usp_GetInterviewerRiskOverviewChannels and usp_QCActivity_CollectRows (evidence).

Fills the CALLER's temp table #ProcessSignalRows (exact shape):

    CREATE TABLE #ProcessSignalRows (
        InterviewId       INT           NOT NULL PRIMARY KEY,
        InterviewerUserId INT           NULL,
        InterviewerEmail  VARCHAR(200)  NULL,
        IrfIntId          INT           NULL,     -- IRF.id
        IRFId             VARCHAR(100)  NULL,     -- IRF code
        InterviewDateIst  DATETIME      NULL,
        Outcome           VARCHAR(20)   NULL,     -- Selected | Rejected | Other
        CommentChars      INT           NULL,
        QuestionCount     INT           NULL,
        VideoMinutes      INT           NULL,
        PlannedMinutes    INT           NULL,
        LateJoinMinutes   INT           NULL,
        CandidateRating   INT           NULL,
        ShortComment      BIT           NOT NULL DEFAULT 0,
        FewQuestions      BIT           NOT NULL DEFAULT 0,
        ShortCall         BIT           NOT NULL DEFAULT 0,
        LateJoin          BIT           NOT NULL DEFAULT 0,
        NegativeRating    BIT           NOT NULL DEFAULT 0
    );

Rules (thresholds in dbo.QCActivitySetting, defaults in brackets; decision D4):
  Short comment   Selected with a comment under PliSelectCommentChars (500), or
                  Rejected under PliRejectCommentChars (250)
  <N questions    fewer than PliMinQuestions (5) QuestionsDiscussed rows on the
                  interview, 0 included (the PLI page skips interviews with none)
  Short call      VideoDuration above 0 and below the plan's DurationSelectedMin;
                  NULL is not flagged
  Late join       first join more than PliLateJoinMinutes (5) after the start, on
                  any completed meeting (the PLI page counts no-show meetings only)
  Negative rating candidate rating below PqiNegativeRating (4)

Window: completed interviews (MeetingStatus 4, 6, 7, 8) whose interviewDate falls
in the IST window, read as a UTC range like usp_GetInterviewerRiskOverview (so
counts line up with the High-Risk page). No all-time counting.
Filters (all optional): @UserIds (comma-separated UserList ids; matched by email
or coded id), @OwnerEmail (channel owner), @IrfIntId (one channel). A caller that
only needs some interviews creates #PsOnly (InterviewId INT PRIMARY KEY) first;
then only those interviews are read (usp_GetHighRiskReports).
Read-only. Optional tables are skipped when missing.
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_InterviewerProcessSignals_Collect]
    @FromIst    DATE,
    @ToIst      DATE,
    @UserIds    NVARCHAR(MAX) = NULL,
    @OwnerEmail VARCHAR(200)  = NULL,
    @IrfIntId   INT           = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'tempdb..#ProcessSignalRows') IS NULL RETURN;
    IF @FromIst IS NULL OR @ToIst IS NULL OR @FromIst > @ToIst RETURN;
    IF DATEDIFF(DAY, @FromIst, @ToIst) > 92 SET @FromIst = DATEADD(DAY, -92, @ToIst);

    SET @UserIds    = NULLIF(LTRIM(RTRIM(@UserIds)), N'');
    SET @OwnerEmail = NULLIF(LTRIM(RTRIM(@OwnerEmail)), '');

    DECLARE @FromUtc DATETIME = DATEADD(MINUTE, -330, CAST(@FromIst AS DATETIME));
    DECLARE @ToUtc   DATETIME = DATEADD(MINUTE, -330, DATEADD(DAY, 1, CAST(@ToIst AS DATETIME)));

    DECLARE @SelectChars INT = 500, @RejectChars INT = 250, @MinQuestions INT = 5, @LateMinutes INT = 5, @NegRating INT = 4;
    IF OBJECT_ID(N'dbo.QCActivitySetting', N'U') IS NOT NULL
        EXEC sp_executesql N'
            SELECT @Sel  = ISNULL(MAX(CASE WHEN Name = ''PliSelectCommentChars'' THEN TRY_CAST(Value AS INT) END), @Sel),
                   @Rej  = ISNULL(MAX(CASE WHEN Name = ''PliRejectCommentChars'' THEN TRY_CAST(Value AS INT) END), @Rej),
                   @MinQ = ISNULL(MAX(CASE WHEN Name = ''PliMinQuestions''       THEN TRY_CAST(Value AS INT) END), @MinQ),
                   @Late = ISNULL(MAX(CASE WHEN Name = ''PliLateJoinMinutes''    THEN TRY_CAST(Value AS INT) END), @Late),
                   @Neg  = ISNULL(MAX(CASE WHEN Name = ''PqiNegativeRating''     THEN TRY_CAST(Value AS INT) END), @Neg)
            FROM dbo.QCActivitySetting',
            N'@Sel INT OUTPUT, @Rej INT OUTPUT, @MinQ INT OUTPUT, @Late INT OUTPUT, @Neg INT OUTPUT',
            @Sel = @SelectChars OUTPUT, @Rej = @RejectChars OUTPUT, @MinQ = @MinQuestions OUTPUT,
            @Late = @LateMinutes OUTPUT, @Neg = @NegRating OUTPUT;

    -- Interviewers asked for (email and coded id both identify them on InterviewMeetings.interviewer).
    CREATE TABLE #PsIds (UserId INT NOT NULL PRIMARY KEY, Email VARCHAR(200) NULL, NickName VARCHAR(200) NULL);
    CREATE TABLE #PsKeys (Interviewer VARCHAR(200) NOT NULL PRIMARY KEY, UserId INT NOT NULL);
    IF @UserIds IS NOT NULL
    BEGIN
        INSERT INTO #PsIds (UserId)
        SELECT DISTINCT TRY_CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@UserIds, N',')
        WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) > 0;

        UPDATE p SET Email = NULLIF(LTRIM(RTRIM(ul.EmailId)), ''),
                     NickName = (SELECT TOP 1 NULLIF(LTRIM(RTRIM(ip.NickName)), '') FROM dbo.InterviewerProfile ip
                                 WHERE ip.UserId = p.UserId ORDER BY ip.Id DESC)
        FROM #PsIds p INNER JOIN dbo.UserList ul ON ul.id = p.UserId;

        INSERT INTO #PsKeys (Interviewer, UserId)
        SELECT k.Interviewer, MIN(k.UserId)
        FROM (SELECT Email AS Interviewer, UserId FROM #PsIds WHERE Email IS NOT NULL
              UNION ALL
              SELECT NickName, UserId FROM #PsIds WHERE NickName IS NOT NULL) k
        GROUP BY k.Interviewer;
    END

    -- Optional interview list from the caller (#PsOnly).
    DECLARE @HasOnly BIT = CASE WHEN OBJECT_ID(N'tempdb..#PsOnly') IS NOT NULL THEN 1 ELSE 0 END;
    CREATE TABLE #PsOnlyLocal (InterviewId INT NOT NULL PRIMARY KEY);
    IF @HasOnly = 1
        EXEC sp_executesql N'INSERT INTO #PsOnlyLocal (InterviewId) SELECT DISTINCT InterviewId FROM #PsOnly WHERE InterviewId IS NOT NULL;';

    ;WITH Done AS (
        SELECT im.id, im.IRFId, LTRIM(RTRIM(im.interviewer)) AS Interviewer, im.interviewDate,
               ic.InterviewStatus, ic.InterviewerComment, ic.VideoDuration,
               ROW_NUMBER() OVER (PARTITION BY im.id ORDER BY ic.id DESC) AS rn
        FROM dbo.InterviewMeetings im
        INNER JOIN dbo.InterviewComments ic ON ic.InterviewId = im.id
        WHERE ic.MeetingStatus IN (4, 6, 7, 8)
          AND im.interviewDate >= @FromUtc
          AND im.interviewDate <  @ToUtc
          AND NULLIF(LTRIM(RTRIM(im.interviewer)), '') IS NOT NULL
          AND (@UserIds IS NULL OR LTRIM(RTRIM(im.interviewer)) IN (SELECT Interviewer FROM #PsKeys))
          AND (@HasOnly = 0 OR im.id IN (SELECT InterviewId FROM #PsOnlyLocal))
    )
    INSERT INTO #ProcessSignalRows (InterviewId, InterviewerUserId, InterviewerEmail, IrfIntId, IRFId, InterviewDateIst,
                                    Outcome, CommentChars, VideoMinutes)
    SELECT d.id,
           k.UserId,
           d.Interviewer,
           irf.id,
           d.IRFId,
           DATEADD(MINUTE, 330, d.interviewDate),
           CASE WHEN d.InterviewStatus IN (2, 4) THEN 'Selected' WHEN d.InterviewStatus IN (3, 5) THEN 'Rejected' ELSE 'Other' END,
           LEN(ISNULL(CAST(d.InterviewerComment AS NVARCHAR(MAX)), N'')),
           TRY_CAST(d.VideoDuration AS INT)
    FROM Done d
    LEFT JOIN #PsKeys k ON k.Interviewer = d.Interviewer
    OUTER APPLY (SELECT TOP 1 i.id, i.RisebirdHr FROM dbo.IRF i WHERE i.IRFId = d.IRFId ORDER BY i.id DESC) irf
    LEFT JOIN dbo.UserList co ON co.id = irf.RisebirdHr
    WHERE d.rn = 1
      AND (@OwnerEmail IS NULL OR LTRIM(RTRIM(co.EmailId)) = @OwnerEmail)
      AND (@IrfIntId IS NULL OR irf.id = @IrfIntId)
      AND NOT EXISTS (SELECT 1 FROM #ProcessSignalRows x WHERE x.InterviewId = d.id)
    OPTION (RECOMPILE);

    -- Interviewer id where the caller did not ask for specific people (by email, else by coded id).
    UPDATE p SET InterviewerUserId = COALESCE(
            (SELECT TOP 1 ul.id FROM dbo.UserList ul WHERE ul.EmailId = p.InterviewerEmail ORDER BY ul.id DESC),
            (SELECT TOP 1 ip.UserId FROM dbo.InterviewerProfile ip WHERE LTRIM(RTRIM(ip.NickName)) = p.InterviewerEmail AND ip.UserId IS NOT NULL ORDER BY ip.Id DESC))
    FROM #ProcessSignalRows p
    WHERE p.InterviewerUserId IS NULL;

    -- Questions discussed (0 included), planned duration, first join, candidate rating.
    UPDATE p SET QuestionCount = (SELECT COUNT(1) FROM dbo.QuestionsDiscussed q WHERE q.InterviewId = p.InterviewId)
    FROM #ProcessSignalRows p;

    UPDATE p SET PlannedMinutes = pt.DurationSelectedMin
    FROM #ProcessSignalRows p
    INNER JOIN dbo.IRF i ON i.id = p.IrfIntId
    CROSS APPLY (SELECT TOP 1 TRY_CAST(x.DurationSelectedMin AS INT) AS DurationSelectedMin
                 FROM dbo.PlanTypeChargesMaster x
                 WHERE x.CompanyType = i.CompanyType AND x.PlanTypeEnumValue = i.Heuristicsscore AND x.CountryType = i.JobCountry) pt;

    UPDATE p SET LateJoinMinutes = j.LateMinutes
    FROM #ProcessSignalRows p
    INNER JOIN dbo.InterviewMeetings im ON im.id = p.InterviewId
    CROSS APPLY (SELECT DATEDIFF(MINUTE, im.interviewDate, MIN(imj.Joinedon)) AS LateMinutes
                 FROM dbo.InterviewMeetingsJoinTime imj
                 WHERE imj.interviewid = p.InterviewId AND imj.Joinedon IS NOT NULL
                 HAVING MIN(imj.Joinedon) IS NOT NULL) j;

    UPDATE p SET CandidateRating = r.Rating
    FROM #ProcessSignalRows p
    CROSS APPLY (SELECT MIN(cfd.Rating) AS Rating FROM dbo.CandidateFeedbackDetails cfd
                 WHERE cfd.InterviewId = p.InterviewId HAVING MIN(cfd.Rating) IS NOT NULL) r;

    UPDATE #ProcessSignalRows SET
        ShortComment   = CASE WHEN (Outcome = 'Selected' AND ISNULL(CommentChars, 0) < @SelectChars)
                                OR (Outcome = 'Rejected' AND ISNULL(CommentChars, 0) < @RejectChars) THEN 1 ELSE 0 END,
        FewQuestions   = CASE WHEN ISNULL(QuestionCount, 0) < @MinQuestions THEN 1 ELSE 0 END,
        ShortCall      = CASE WHEN VideoMinutes > 0 AND PlannedMinutes > 0 AND VideoMinutes < PlannedMinutes THEN 1 ELSE 0 END,
        LateJoin       = CASE WHEN LateJoinMinutes > @LateMinutes THEN 1 ELSE 0 END,
        NegativeRating = CASE WHEN CandidateRating IS NOT NULL AND CandidateRating < @NegRating THEN 1 ELSE 0 END;

    DROP TABLE IF EXISTS #PsOnlyLocal;
    DROP TABLE IF EXISTS #PsKeys;
    DROP TABLE IF EXISTS #PsIds;
END
GO
