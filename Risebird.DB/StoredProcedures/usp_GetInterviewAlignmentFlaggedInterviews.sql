IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviewAlignmentFlaggedInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewAlignmentFlaggedInterviews
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- "What went wrong before" on the interview alignment page: the signed-in interviewer's own past interviews on
-- this channel where any quality signal fired. Ported from the flagged-interviews part of the earlier
-- usp_GetInterviewAlignmentDetails (branch dm-modify); nothing else of that procedure is used.
--
-- Read-only. It writes nothing and changes no existing object. It returns rows only when @InterviewerEmail is
-- the interviewer of @InterviewId, and only that interviewer's own interviews.
--
-- Signals (the existing data conventions):
--   IsSuspiciousQC                 - latest QC form found a suspicious app or red flags
--   IsRedo                         - the interview was redone, or a redo was approved
--   IsLowCandidateRating           - the candidate rated the interview 2 or below
--   IsNegativeAIReportVerdict      - InterviewComments.AIVerdict = 'no'   (on this field 'no' is bad)
--   IsNegativeAITranscriptVerdict  - InterviewComments.AITranscriptVerdict = 'yes' (on this field 'yes' is bad)
-- RiskCategory: 3+ signals High, 2 Medium, 1 Low.
-- Rows: the most recent @RowLimit flagged interviews OR anything within @LookbackDays, whichever is more.
--
-- CandidateEmail is returned only so the application can look up the candidate's coded name
-- (CandidatePseudonym); it is never rendered.
CREATE PROCEDURE [dbo].[usp_GetInterviewAlignmentFlaggedInterviews]
    @InterviewId      INT,
    @InterviewerEmail VARCHAR(200),
    @TimeNow          DATETIME = NULL,
    @RowLimit         INT = 10,
    @LookbackDays     INT = 90
AS
BEGIN
    SET NOCOUNT ON;

    IF @TimeNow IS NULL SET @TimeNow = GETDATE();
    IF @RowLimit IS NULL OR @RowLimit <= 0 SET @RowLimit = 10;
    IF @LookbackDays IS NULL OR @LookbackDays < 0 SET @LookbackDays = 90;

    DECLARE @Email   VARCHAR(200) = LTRIM(RTRIM(REPLACE(ISNULL(@InterviewerEmail, ''), '"', '')));
    DECLARE @IRFCode VARCHAR(50);

    -- The gate: the caller must be this interview's interviewer.
    SELECT TOP 1 @IRFCode = im.IRFId
      FROM dbo.InterviewMeetings im
     WHERE im.id = @InterviewId
       AND LTRIM(RTRIM(REPLACE(ISNULL(im.interviewer, ''), '"', ''))) = @Email;

    IF @IRFCode IS NULL OR @Email = ''
    BEGIN
        -- Same shape, no rows.
        SELECT CAST(NULL AS INT) AS InterviewId, CAST(NULL AS DATETIME) AS InterviewDate, CAST(NULL AS VARCHAR(200)) AS CandidateEmail,
               CAST(NULL AS INT) AS SignalCount, CAST(NULL AS VARCHAR(10)) AS RiskCategory,
               CAST(NULL AS BIT) AS IsSuspiciousQC, CAST(NULL AS BIT) AS IsRedo, CAST(NULL AS BIT) AS IsLowCandidateRating,
               CAST(NULL AS BIT) AS IsNegativeAIReportVerdict, CAST(NULL AS BIT) AS IsNegativeAITranscriptVerdict,
               CAST(NULL AS INT) AS CandidateRating, CAST(NULL AS INT) AS AIReportQCAnalysisScore, CAST(NULL AS INT) AS AITranscriptAnalysisScore,
               CAST(NULL AS VARCHAR(600)) AS AIReasoning, CAST(NULL AS VARCHAR(400)) AS QCComment
         WHERE 1 = 0;
        RETURN;
    END

    ;WITH Past AS (
        SELECT
            im.id                                   AS InterviewId,
            im.interviewDate                        AS InterviewDate,
            LTRIM(RTRIM(im.candidateEmail))         AS CandidateEmail,
            CAST(CASE WHEN ISNULL(qc.IsSuspiciousappfound, 0) = 1 OR ISNULL(qc.Redflags, 0) > 0 THEN 1 ELSE 0 END AS BIT) AS IsSuspiciousQC,
            CAST(CASE WHEN ISNULL(im.RedoInterview, 0) = 1 OR ISNULL(ri.Approved, 0) = 1 THEN 1 ELSE 0 END AS BIT)      AS IsRedo,
            CAST(CASE WHEN cf.Rating IS NOT NULL AND cf.Rating <= 2 THEN 1 ELSE 0 END AS BIT)                            AS IsLowCandidateRating,
            CAST(CASE WHEN LOWER(ISNULL(ic.AIVerdict, '')) = 'no' THEN 1 ELSE 0 END AS BIT)                              AS IsNegativeAIReportVerdict,
            CAST(CASE WHEN LOWER(ISNULL(ic.AITranscriptVerdict, '')) = 'yes' THEN 1 ELSE 0 END AS BIT)                   AS IsNegativeAITranscriptVerdict,
            cf.Rating                               AS CandidateRating,
            ic.AIReportQCAnalysisScore              AS AIReportQCAnalysisScore,
            ic.AITranscriptAnalysisScore            AS AITranscriptAnalysisScore,
            LEFT(ic.AITranscriptAnalysis, 600)      AS AIReasoning,
            LEFT(ic.QCComment, 400)                 AS QCComment
          FROM dbo.InterviewMeetings im
          LEFT JOIN dbo.InterviewComments ic ON ic.InterviewId = im.id
          OUTER APPLY (SELECT TOP 1 q.IsSuspiciousappfound, q.Redflags
                         FROM dbo.Trans_QCFormReport q
                        WHERE q.InterviewId = im.id
                        ORDER BY q.id DESC) qc
          OUTER APPLY (SELECT TOP 1 r.Approved
                         FROM dbo.RedoInterviews r
                        WHERE r.InterviewId = im.id
                        ORDER BY r.id DESC) ri
          OUTER APPLY (SELECT TOP 1 f.Rating
                         FROM dbo.CandidateFeedbackDetails f
                        WHERE f.InterviewId = im.id
                        ORDER BY f.Id DESC) cf
         WHERE im.IRFId = @IRFCode
           AND LTRIM(RTRIM(REPLACE(ISNULL(im.interviewer, ''), '"', ''))) = @Email
           AND ISNULL(im.confirmationStatus, 0) = 1
           AND im.interviewDate < @TimeNow
           AND im.id <> @InterviewId
    ), Flagged AS (
        SELECT *,
               CAST(IsSuspiciousQC AS INT) + CAST(IsRedo AS INT) + CAST(IsLowCandidateRating AS INT)
             + CAST(IsNegativeAIReportVerdict AS INT) + CAST(IsNegativeAITranscriptVerdict AS INT) AS SignalCount
          FROM Past
    ), Ranked AS (
        SELECT *, ROW_NUMBER() OVER (ORDER BY InterviewDate DESC, InterviewId DESC) AS RowNo
          FROM Flagged
         WHERE SignalCount > 0
    )
    SELECT
        InterviewId,
        InterviewDate,
        CandidateEmail,
        SignalCount,
        CASE WHEN SignalCount >= 3 THEN 'High' WHEN SignalCount = 2 THEN 'Medium' ELSE 'Low' END AS RiskCategory,
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
     WHERE RowNo <= @RowLimit
        OR InterviewDate >= DATEADD(DAY, -@LookbackDays, @TimeNow)
     ORDER BY InterviewDate DESC, InterviewId DESC;
END
GO
