/*
================================================================================
usp_GetInterviewerProcessSignals — PLI / PQI signals in a window (QC Tracking,
Delta 5). Rules and thresholds: usp_InterviewerProcessSignals_Collect.

Result set 1  per interview (report flags): InterviewId, InterviewerUserId,
              InterviewerEmail, IrfIntId, IRFId, InterviewDateIst, Outcome,
              CommentChars, QuestionCount, VideoMinutes, PlannedMinutes,
              LateJoinMinutes, CandidateRating, ShortComment, FewQuestions,
              ShortCall, LateJoin, NegativeRating.
Result set 2  per interviewer x IRF: counts of each signal, Completed, Selected,
              Rejected, SelectPct, RejectPct, ProcessLapses (the five PLI kinds:
              short select comment, short reject comment, <N questions, short
              call, late join), HighSelection / HighRejection (PqiSelectPct /
              PqiRejectPct with at least PqiMinInterviews completed).
Result set 3  per interviewer: the same, rates recomputed from the totals (never
              an average of per-channel rates), so the rollup equals the sum of
              the channels.

@UserIds    comma-separated UserList ids (NULL = everyone in scope).
@OwnerEmail channel owner scope (HR / SeniorHR see only their channels).
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_GetInterviewerProcessSignals]
    @FromIst    DATE = NULL,
    @ToIst      DATE = NULL,
    @UserIds    NVARCHAR(MAX) = NULL,
    @OwnerEmail VARCHAR(200)  = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @ToIst IS NULL SET @ToIst = CAST(DATEADD(MINUTE, 330, GETUTCDATE()) AS DATE);
    IF @FromIst IS NULL OR @FromIst > @ToIst SET @FromIst = DATEADD(DAY, -30, @ToIst);

    DECLARE @SelectPct INT = 40, @RejectPct INT = 80, @MinInterviews INT = 5;
    IF OBJECT_ID(N'dbo.QCActivitySetting', N'U') IS NOT NULL
        EXEC sp_executesql N'
            SELECT @Sel = ISNULL(MAX(CASE WHEN Name = ''PqiSelectPct''     THEN TRY_CAST(Value AS INT) END), @Sel),
                   @Rej = ISNULL(MAX(CASE WHEN Name = ''PqiRejectPct''     THEN TRY_CAST(Value AS INT) END), @Rej),
                   @Min = ISNULL(MAX(CASE WHEN Name = ''PqiMinInterviews'' THEN TRY_CAST(Value AS INT) END), @Min)
            FROM dbo.QCActivitySetting',
            N'@Sel INT OUTPUT, @Rej INT OUTPUT, @Min INT OUTPUT',
            @Sel = @SelectPct OUTPUT, @Rej = @RejectPct OUTPUT, @Min = @MinInterviews OUTPUT;

    CREATE TABLE #ProcessSignalRows (
        InterviewId       INT           NOT NULL PRIMARY KEY,
        InterviewerUserId INT           NULL,
        InterviewerEmail  VARCHAR(200)  NULL,
        IrfIntId          INT           NULL,
        IRFId             VARCHAR(100)  NULL,
        InterviewDateIst  DATETIME      NULL,
        Outcome           VARCHAR(20)   NULL,
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
        NegativeRating    BIT           NOT NULL DEFAULT 0,
        IsBad             BIT           NULL      -- this procedure only: graded P0-P5 (not filled by the collector)
    );

    EXEC dbo.usp_InterviewerProcessSignals_Collect
        @FromIst = @FromIst, @ToIst = @ToIst, @UserIds = @UserIds, @OwnerEmail = @OwnerEmail;

    UPDATE p SET IsBad = 1
    FROM #ProcessSignalRows p
    WHERE EXISTS (SELECT 1 FROM dbo.RiskCategory rc
                  WHERE rc.InterviewId = p.InterviewId
                    AND UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) LIKE 'P[0-5]');

    /* Result set 1: per interview */
    SELECT InterviewId, InterviewerUserId, InterviewerEmail, IrfIntId, IRFId, InterviewDateIst, Outcome,
           CommentChars, QuestionCount, VideoMinutes, PlannedMinutes, LateJoinMinutes, CandidateRating,
           ShortComment, FewQuestions, ShortCall, LateJoin, NegativeRating
    FROM #ProcessSignalRows
    ORDER BY InterviewDateIst DESC, InterviewId DESC;

    SELECT
        p.InterviewerUserId,
        p.IrfIntId,
        MAX(p.IRFId) AS IRFId,
        COUNT(1) AS Completed,
        SUM(CASE WHEN p.Outcome = 'Selected' THEN 1 ELSE 0 END) AS Selected,
        SUM(CASE WHEN p.Outcome = 'Rejected' THEN 1 ELSE 0 END) AS Rejected,
        SUM(CASE WHEN p.ShortComment = 1 AND p.Outcome = 'Selected' THEN 1 ELSE 0 END) AS ShortSelectComments,
        SUM(CASE WHEN p.ShortComment = 1 AND p.Outcome = 'Rejected' THEN 1 ELSE 0 END) AS ShortRejectComments,
        SUM(CAST(p.FewQuestions AS INT)) AS FewQuestions,
        SUM(CAST(p.ShortCall AS INT)) AS ShortCalls,
        SUM(CAST(p.LateJoin AS INT)) AS LateJoins,
        SUM(CAST(p.NegativeRating AS INT)) AS NegativeRatings,
        -- Graded bad reports (RiskCategory P0-P5): "share of bad" on the channel rows
        SUM(CAST(ISNULL(p.IsBad, 0) AS INT)) AS Bad
    INTO #PerIrf
    FROM #ProcessSignalRows p
    WHERE p.InterviewerUserId IS NOT NULL
    GROUP BY p.InterviewerUserId, p.IrfIntId;

    /* Result set 2: per interviewer x IRF */
    SELECT
        x.InterviewerUserId, x.IrfIntId, x.IRFId, x.Completed, x.Selected, x.Rejected,
        CAST(CASE WHEN x.Completed = 0 THEN NULL ELSE x.Selected * 100.0 / x.Completed END AS DECIMAL(6,1)) AS SelectPct,
        CAST(CASE WHEN x.Completed = 0 THEN NULL ELSE x.Rejected * 100.0 / x.Completed END AS DECIMAL(6,1)) AS RejectPct,
        x.ShortSelectComments, x.ShortRejectComments, x.FewQuestions, x.ShortCalls, x.LateJoins, x.NegativeRatings, x.Bad,
        x.ShortSelectComments + x.ShortRejectComments + x.FewQuestions + x.ShortCalls + x.LateJoins AS ProcessLapses,
        CAST(CASE WHEN x.Completed >= @MinInterviews AND x.Selected * 100.0 / NULLIF(x.Completed, 0) >= @SelectPct THEN 1 ELSE 0 END AS BIT) AS HighSelection,
        CAST(CASE WHEN x.Completed >= @MinInterviews AND x.Rejected * 100.0 / NULLIF(x.Completed, 0) >= @RejectPct THEN 1 ELSE 0 END AS BIT) AS HighRejection
    FROM #PerIrf x
    ORDER BY x.InterviewerUserId, x.IrfIntId;

    /* Result set 3: per interviewer (rates recomputed from totals) */
    SELECT
        t.InterviewerUserId, t.Channels, t.Completed, t.Selected, t.Rejected,
        CAST(CASE WHEN t.Completed = 0 THEN NULL ELSE t.Selected * 100.0 / t.Completed END AS DECIMAL(6,1)) AS SelectPct,
        CAST(CASE WHEN t.Completed = 0 THEN NULL ELSE t.Rejected * 100.0 / t.Completed END AS DECIMAL(6,1)) AS RejectPct,
        t.ShortSelectComments, t.ShortRejectComments,
        t.ShortSelectComments + t.ShortRejectComments AS ShortComments,
        t.FewQuestions, t.ShortCalls, t.LateJoins, t.NegativeRatings, t.Bad,
        t.ShortSelectComments + t.ShortRejectComments + t.FewQuestions + t.ShortCalls + t.LateJoins AS ProcessLapses,
        CAST(CASE WHEN t.Completed >= @MinInterviews AND t.Selected * 100.0 / NULLIF(t.Completed, 0) >= @SelectPct THEN 1 ELSE 0 END AS BIT) AS HighSelection,
        CAST(CASE WHEN t.Completed >= @MinInterviews AND t.Rejected * 100.0 / NULLIF(t.Completed, 0) >= @RejectPct THEN 1 ELSE 0 END AS BIT) AS HighRejection
    FROM (
        SELECT InterviewerUserId, COUNT(1) AS Channels, SUM(Completed) AS Completed, SUM(Selected) AS Selected,
               SUM(Rejected) AS Rejected, SUM(ShortSelectComments) AS ShortSelectComments,
               SUM(ShortRejectComments) AS ShortRejectComments, SUM(FewQuestions) AS FewQuestions,
               SUM(ShortCalls) AS ShortCalls, SUM(LateJoins) AS LateJoins, SUM(NegativeRatings) AS NegativeRatings,
               SUM(Bad) AS Bad
        FROM #PerIrf
        GROUP BY InterviewerUserId
    ) t
    ORDER BY t.InterviewerUserId;

    DROP TABLE IF EXISTS #PerIrf;
    DROP TABLE IF EXISTS #ProcessSignalRows;
END
GO
