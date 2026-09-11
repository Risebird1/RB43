IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewAlignmentDetails'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetInterviewAlignmentDetails]
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================================================
-- Interview Alignment page - one round trip for all six content sections.
--
-- Returns SIX result sets, in this order. MeetingRespository.GetInterviewAlignmentDetails
-- reads them POSITIONALLY through a Dapper GridReader - reordering them here breaks the
-- mapping silently rather than loudly.
--
--   1. Interview + candidate context      0 or 1 row
--   2. Rubric skills                      0..n
--   3. Sample questions                   0..n
--   4. Client notes                       0..20
--   5. Flagged past interviews            0..n
--   6. Performance summary                always exactly 1 row
--
-- AUTHORISATION IS IN THIS PROCEDURE. @IrfIntId is resolved by a SELECT that requires
-- im.interviewer = @InterviewerEmail. For any other caller it stays NULL, every set
-- filters to zero rows, and set 6 returns one all-zero row. An empty set 1 is the
-- signal the C# layer keys on; it does not re-check the assignment.
--
-- Read-only throughout. No table is written.
-- =============================================================================
CREATE PROCEDURE [dbo].[usp_GetInterviewAlignmentDetails]
    @InterviewId         int,
    @InterviewerEmail    varchar(200),
    @TimeNow             datetime = null,
    @FlaggedRowLimit     int      = 10,
    @FlaggedLookbackDays int      = 90
AS
BEGIN
    SET NOCOUNT ON;

    IF @TimeNow IS NULL SET @TimeNow = GETDATE();
    IF @FlaggedRowLimit IS NULL OR @FlaggedRowLimit <= 0 SET @FlaggedRowLimit = 10;
    IF @FlaggedLookbackDays IS NULL OR @FlaggedLookbackDays < 0 SET @FlaggedLookbackDays = 90;

    DECLARE @IrfIntId   int,
            @IRFCode    varchar(50),
            @IsFirst    bit = 0;

    -- The gate. Requires the caller to be the assigned interviewer.
    SELECT TOP 1
           @IrfIntId = irf.id,
           @IRFCode  = im.IRFId
      FROM InterviewMeetings im
      JOIN IRF irf ON irf.IRFId = im.IRFId
     WHERE im.id = @InterviewId
       AND im.interviewer = @InterviewerEmail;

    -- "First interview on this IRF" is by interviewDate, not id, so the flag is stable
    -- when interviews are booked out of order.
    IF @IrfIntId IS NOT NULL
    BEGIN
        IF NOT EXISTS (
                SELECT 1
                  FROM InterviewMeetings prev
                  JOIN InterviewMeetings cur ON cur.id = @InterviewId
                 WHERE prev.IRFId = cur.IRFId
                   AND prev.interviewer = @InterviewerEmail
                   AND prev.confirmationStatus = 1
                   AND prev.id <> cur.id
                   AND prev.interviewDate < cur.interviewDate)
            SET @IsFirst = 1;
    END

    -- ---------------------------------------------------------------------
    -- 1. Interview + candidate context
    --    No candidateName, candidateEmail, Phone or Candidate.Name is selected.
    -- ---------------------------------------------------------------------
    SELECT
        im.id                                       AS InterviewId,
        irf.id                                      AS IrfIntId,
        im.IRFId                                    AS IRFCode,
        CASE WHEN cm.id IS NOT NULL
             THEN 'AC' + RIGHT('0000' + CAST(cm.id AS varchar(10)), 4)
             ELSE 'AX' + RIGHT('0000' + CAST(im.id AS varchar(10)), 4)
        END                                         AS CandidateCodedName,
        CASE WHEN ISNULL(irf.SkillSetsevaluate, 1) > 1 AND ISNULL(irf.JDTitleTwo, '') <> ''
             THEN irf.JDTiltle + ' and ' + irf.JDTitleTwo
             ELSE irf.JDTiltle
        END                                         AS RoleTitle,
        cl.[Name]                                   AS ClientName,
        COALESCE(NULLIF(ca.CodedResumeLink, ''), NULLIF(ca.EncodedResumeLink, '')) AS CandidateCodedResumeLink,
        COALESCE(NULLIF(irf.InterviewDuration, 0), im.interviewDuration) AS ExpectedDurationMinutes,
        im.interviewDate                            AS InterviewDate,
        im.TimeZonesValue                           AS CandidateTimeZoneLabel,
        im.TimeZoneoffset                           AS CandidateTimeZoneOffsetMinutes,
        CAST(ISNULL(irf.AITrialEnabled, 0) AS bit)  AS AiTrialEnabled,
        CAST(@IsFirst AS bit)                       AS IsFirstInterviewOnIrf,
        CAST(ISNULL(ip.TrialOnboardingAcknowledged, 0) AS bit) AS TrialOnboardingAcknowledged,
        ip.TrialOnboardingPolicyVersion             AS TrialOnboardingPolicyVersion,
        ip.NickName                                 AS InterviewerCodedName,
        CAST(ISNULL(irf.IsVideoRequired, 0) AS bit) AS IsVideoRequired,
        irf.TechRubricType                          AS TechRubricType
      FROM InterviewMeetings im
      JOIN IRF irf ON irf.IRFId = im.IRFId
      LEFT JOIN Client cl ON cl.id = irf.Clientid
      LEFT JOIN UserList ulc ON ulc.EmailId = im.candidateEmail
      LEFT JOIN CandidatesMaster cm ON cm.UserId = ulc.id
      LEFT JOIN Candidate ca ON ca.CandidateMasterId = cm.id AND ca.IRFId = irf.id
      LEFT JOIN UserList uli ON uli.EmailId = im.interviewer
      LEFT JOIN InterviewerProfile ip ON ip.UserId = uli.id
     WHERE im.id = @InterviewId
       AND @IrfIntId IS NOT NULL;

    -- ---------------------------------------------------------------------
    -- 2. Rubric skills. Primary stack before secondary, heaviest first.
    -- ---------------------------------------------------------------------
    SELECT
        tts.id                                        AS Id,
        tts.SkillId                                   AS SkillId,
        COALESCE(NULLIF(tts.SkillName, ''), sk.[Name]) AS SkillName,
        tts.LevelId                                   AS LevelId,
        tts.Weightage                                 AS Weightage,
        CAST(ISNULL(tts.isEliminationskill, 0) AS bit) AS IsEliminationSkill,
        ISNULL(tts.SkillSet, 1)                       AS SkillSet,
        tts.Concepts                                  AS Concepts,
        tts.Natureofquestion                          AS NatureOfQuestion,
        tts.Timelimit                                 AS TimeLimit
      FROM TransTechSkill tts
      LEFT JOIN Skill sk ON sk.id = tts.SkillId
     WHERE tts.IRFId = @IrfIntId
       AND @IrfIntId IS NOT NULL
     ORDER BY ISNULL(tts.SkillSet, 1), ISNULL(tts.Weightage, 0) DESC, tts.id;

    -- ---------------------------------------------------------------------
    -- 3. Sample questions.
    --    VideoLink is aliased from the EXISTING AIQuestionVideoLink column rather than a
    --    new one - the table already carries the AI question video, so adding a second
    --    column for the same thing would leave two places to populate and one of them
    --    always empty. ThumbnailLink is genuinely new.
    -- ---------------------------------------------------------------------
    SELECT
        scsq.id                                          AS Id,
        scsq.Skill                                       AS SkillId,
        COALESCE(NULLIF(scsq.SkillName, ''), sk.[Name])  AS SkillName,
        scsq.Comment                                     AS QuestionText,
        scsq.ExpectedAnswer                              AS ExpectedAnswer,
        scsq.AIQuestionVideoLink                         AS VideoLink,
        scsq.ThumbnailLink                               AS ThumbnailLink
      FROM SkillChannelSampleQuestions scsq
      LEFT JOIN Skill sk ON sk.id = scsq.Skill
     WHERE scsq.IRFId = @IrfIntId
       AND @IrfIntId IS NOT NULL
       AND ISNULL(scsq.Comment, '') <> ''
     ORDER BY COALESCE(NULLIF(scsq.SkillName, ''), sk.[Name]), scsq.id;

    -- ---------------------------------------------------------------------
    -- 4. Client notes. IRF-wide instructions plus notes raised against THIS interview -
    --    never another candidate's note.
    -- ---------------------------------------------------------------------
    SELECT TOP 20
        cbm.id                                   AS Id,
        cbm.[Subject]                            AS [Subject],
        cbm.[Message]                            AS [Message],
        cbm.SentAt                               AS SentAt,
        CAST(CASE WHEN cbm.InterviewId IS NULL THEN 0 ELSE 1 END AS bit) AS IsInterviewSpecific
      FROM ClientBroadcastMessages cbm
     WHERE cbm.IRFId = @IrfIntId
       AND @IrfIntId IS NOT NULL
       AND (cbm.InterviewId IS NULL OR cbm.InterviewId = @InterviewId)
     ORDER BY cbm.SentAt DESC, cbm.id DESC;

    -- ---------------------------------------------------------------------
    -- The population behind BOTH set 5 and set 6, built once so the detail rows and the
    -- aggregate can never disagree about what counts as flagged.
    --
    -- MeetingStatus and InterviewStatus live on InterviewComments, not InterviewMeetings.
    -- The status codes match usp_GetInterviewerStatsAndRatings so the numbers agree with
    -- the DM Approval metrics over the same population.
    -- ---------------------------------------------------------------------
    CREATE TABLE #PastInterviews (
        InterviewId                  int,
        IrfIntId                     int,
        InterviewDate                datetime,
        CandidateCodedName           varchar(20),
        MeetingStatus                int NULL,
        InterviewStatus              int NULL,
        IsSuspiciousQC               bit,
        IsRedo                       bit,
        IsLowCandidateRating         bit,
        IsNegativeAIReportVerdict    bit,
        IsNegativeAITranscriptVerdict bit,
        CandidateRating              int NULL,
        AIReportQCAnalysisScore      int NULL,
        AITranscriptAnalysisScore    int NULL,
        AIReasoning                  varchar(600) NULL,
        QCComment                    varchar(400) NULL
    );

    IF @IrfIntId IS NOT NULL
    BEGIN
        INSERT INTO #PastInterviews
        SELECT
            im.id,
            @IrfIntId,
            im.interviewDate,
            CASE WHEN cm.id IS NOT NULL
                 THEN 'AC' + RIGHT('0000' + CAST(cm.id AS varchar(10)), 4)
                 ELSE 'AX' + RIGHT('0000' + CAST(im.id AS varchar(10)), 4)
            END,
            ic.MeetingStatus,
            ic.InterviewStatus,
            CAST(CASE WHEN ISNULL(qc.IsSuspiciousappfound, 0) = 1 OR ISNULL(qc.Redflags, 0) > 0
                      THEN 1 ELSE 0 END AS bit),
            CAST(CASE WHEN ISNULL(im.RedoInterview, 0) = 1 OR ISNULL(ri.Approved, 0) = 1
                      THEN 1 ELSE 0 END AS bit),
            CAST(CASE WHEN cf.Rating IS NOT NULL AND cf.Rating <= 2 THEN 1 ELSE 0 END AS bit),
            -- The two AI verdicts have OPPOSITE polarity. 'No' is bad on AIVerdict;
            -- 'yes' is bad on AITranscriptVerdict. That is the existing data convention.
            CAST(CASE WHEN LOWER(ISNULL(ic.AIVerdict, '')) = 'no' THEN 1 ELSE 0 END AS bit),
            CAST(CASE WHEN LOWER(ISNULL(ic.AITranscriptVerdict, '')) = 'yes' THEN 1 ELSE 0 END AS bit),
            cf.Rating,
            ic.AIReportQCAnalysisScore,
            ic.AITranscriptAnalysisScore,
            LEFT(ic.AITranscriptAnalysis, 600),
            LEFT(ic.QCComment, 400)
          FROM InterviewMeetings im
          LEFT JOIN InterviewComments ic ON ic.InterviewId = im.id
          LEFT JOIN UserList ulc ON ulc.EmailId = im.candidateEmail
          LEFT JOIN CandidatesMaster cm ON cm.UserId = ulc.id
          OUTER APPLY (SELECT TOP 1 q.IsSuspiciousappfound, q.Redflags
                         FROM Trans_QCFormReport q
                        WHERE q.InterviewId = im.id
                        ORDER BY q.id DESC) qc
          OUTER APPLY (SELECT TOP 1 r.Approved
                         FROM RedoInterviews r
                        WHERE r.InterviewId = im.id
                        ORDER BY r.id DESC) ri
          OUTER APPLY (SELECT TOP 1 f.Rating
                         FROM CandidateFeedbackDetails f
                        WHERE f.InterviewId = im.id
                        ORDER BY f.Id DESC) cf
         WHERE im.IRFId = @IRFCode
           AND im.interviewer = @InterviewerEmail
           AND im.confirmationStatus = 1
           AND im.interviewDate < @TimeNow
           AND im.id <> @InterviewId;
    END

    -- ---------------------------------------------------------------------
    -- 5. Flagged past interviews - any signal fired.
    --    Row limit is a UNION of the two bounds, not an intersection: the most recent
    --    @FlaggedRowLimit OR anything inside @FlaggedLookbackDays, whichever is more
    --    inclusive.
    -- ---------------------------------------------------------------------
    ;WITH Flagged AS (
        SELECT *,
               (CAST(IsSuspiciousQC AS int) + CAST(IsRedo AS int) + CAST(IsLowCandidateRating AS int)
                + CAST(IsNegativeAIReportVerdict AS int) + CAST(IsNegativeAITranscriptVerdict AS int)) AS SignalCount
          FROM #PastInterviews
    ), Ranked AS (
        SELECT *, ROW_NUMBER() OVER (ORDER BY InterviewDate DESC) AS RowNo
          FROM Flagged
         WHERE SignalCount > 0
    )
    SELECT
        InterviewId,
        IrfIntId,
        InterviewDate,
        CandidateCodedName,
        SignalCount,
        CASE WHEN SignalCount >= 3 THEN 'High'
             WHEN SignalCount = 2  THEN 'Medium'
             ELSE 'Low' END                    AS RiskCategory,
        IsSuspiciousQC,
        IsRedo,
        IsLowCandidateRating,
        IsNegativeAIReportVerdict,
        IsNegativeAITranscriptVerdict,
        CandidateRating,
        AIReportQCAnalysisScore,
        AITranscriptAnalysisScore,
        AIReasoning,
        QCComment
      FROM Ranked
     WHERE RowNo <= @FlaggedRowLimit
        OR InterviewDate >= DATEADD(day, -@FlaggedLookbackDays, @TimeNow)
     ORDER BY InterviewDate DESC;

    -- ---------------------------------------------------------------------
    -- 6. Performance summary. Always one row.
    --    Every SUM is ISNULL-wrapped: SUM over an empty set returns NULL, and Dapper
    --    throws mapping NULL into the non-nullable int counters. The AVGs are left
    --    nullable on purpose - the view renders "-" when there is nothing to average.
    -- ---------------------------------------------------------------------
    SELECT
        ISNULL(COUNT(*), 0)                                                      AS TotalInterviews,
        ISNULL(SUM(CASE WHEN MeetingStatus IN (4,6,7,8) THEN 1 ELSE 0 END), 0)   AS TotalCompleted,
        ISNULL(SUM(CASE WHEN InterviewStatus = 2 AND MeetingStatus IN (4,6) THEN 1 ELSE 0 END), 0) AS TotalSelects,
        ISNULL(SUM(CASE WHEN InterviewStatus = 3 AND MeetingStatus IN (4,7) THEN 1 ELSE 0 END), 0) AS TotalRejects,
        CAST(AVG(CAST(AIReportQCAnalysisScore AS decimal(9,2))) AS decimal(9,2)) AS AverageAIReportScore,
        CAST(AVG(CAST(AITranscriptAnalysisScore AS decimal(9,2))) AS decimal(9,2)) AS AverageAITranscriptScore,
        ISNULL(SUM(CASE WHEN AIReportQCAnalysisScore IS NOT NULL THEN 1 ELSE 0 END), 0) AS AIReportScoredInterviews,
        ISNULL(SUM(CAST(IsNegativeAIReportVerdict AS int)), 0)                   AS NegativeAIReportVerdicts,
        CAST(AVG(CAST(CandidateRating AS decimal(9,2))) AS decimal(9,2))         AS AverageCandidateRating,
        ISNULL(SUM(CASE WHEN CandidateRating IS NOT NULL THEN 1 ELSE 0 END), 0)  AS CandidateRatingResponses,
        ISNULL(SUM(CAST(IsSuspiciousQC AS int)), 0)                              AS SuspiciousCount,
        ISNULL(SUM(CAST(IsRedo AS int)), 0)                                      AS RedoCount,
        ISNULL(SUM(CAST(IsLowCandidateRating AS int)), 0)                        AS LowRatedCount,
        ISNULL(SUM(CAST(IsNegativeAITranscriptVerdict AS int)), 0)               AS NegativeAIVerdictCount,
        ISNULL(SUM(CASE WHEN (CAST(IsSuspiciousQC AS int) + CAST(IsRedo AS int)
                            + CAST(IsLowCandidateRating AS int)
                            + CAST(IsNegativeAIReportVerdict AS int)
                            + CAST(IsNegativeAITranscriptVerdict AS int)) >= 2
                        THEN 1 ELSE 0 END), 0)                                   AS ConcerningRiskCount
      FROM #PastInterviews;

    DROP TABLE #PastInterviews;
END
GO
