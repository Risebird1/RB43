/*
================================================================================
usp_GetInterviewerExposure — raw evidence for the coaching brief
(InterviewerCoachingBriefBuilder groups it into themes in C#).

Result sets:
  1 Profile   interviewer + coaching status (sessions in range, last session,
              last summary email for the 12-hour cooldown)
  2 Risk      RiskCategory checks on the interviewer's interviews in the range
              (the same checks the IRF Audit High-Risk Interviewers tab counts)
  3 Payment   Payment SLA deductions > 0 on those interviews, with the reason columns
              and who last saved them (AISystem = the AI review)
  4 Trial     trial flags (factor passes and values) raised in the range or still pending
  5 Standing  internal channel standing: auto-QC verdicts / audit removals,
              red markers, Total Interviews status

Interview dates are compared as IST (InterviewMeetings.interviewDate).
Optional tables are read behind OBJECT_ID / COL_LENGTH checks.
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_GetInterviewerExposure]
    @InterviewerEmail VARCHAR(150),
    @FromIst          DATE,
    @ToIst            DATE
AS
BEGIN
    SET NOCOUNT ON;

    SET @InterviewerEmail = NULLIF(LTRIM(RTRIM(@InterviewerEmail)), '');

    DECLARE @UserId INT = (SELECT TOP 1 id FROM dbo.UserList WHERE EmailId = @InterviewerEmail ORDER BY id DESC);
    DECLARE @ProfileId INT = (SELECT TOP 1 Id FROM dbo.InterviewerProfile WHERE UserId = @UserId ORDER BY Id DESC);
    DECLARE @InterviewerId INT = (SELECT TOP 1 id FROM dbo.Interviewer WHERE UserId = @UserId ORDER BY id DESC);

    DECLARE @FromIstDt DATETIME = CAST(@FromIst AS DATETIME);
    DECLARE @ToIstDt   DATETIME = DATEADD(DAY, 1, CAST(@ToIst AS DATETIME));
    DECLARE @FromUtc   DATETIME = DATEADD(MINUTE, -330, @FromIstDt);
    DECLARE @ToUtc     DATETIME = DATEADD(MINUTE, -330, @ToIstDt);

    DECLARE @HasCoaching BIT = CASE WHEN OBJECT_ID(N'dbo.InterviewerCoachingSession', N'U') IS NOT NULL THEN 1 ELSE 0 END;

    /* ---------- 1 Profile ---------- */
    DECLARE @Sessions INT = 0, @LastCoachedAtUtc DATETIME = NULL, @LastOutcome VARCHAR(30) = NULL,
            @LastCoach VARCHAR(150) = NULL, @LastEmailAtUtc DATETIME = NULL;

    IF @HasCoaching = 1 AND @UserId IS NOT NULL
        EXEC sp_executesql N'
            SELECT @Sessions = COUNT(*) FROM dbo.InterviewerCoachingSession
            WHERE InterviewerUserId = @UserId AND CreatedAtUtc >= @FromUtc AND CreatedAtUtc < @ToUtc;
            SELECT TOP 1 @LastCoachedAtUtc = CreatedAtUtc, @LastOutcome = Outcome, @LastCoach = CoachEmail
            FROM dbo.InterviewerCoachingSession WHERE InterviewerUserId = @UserId ORDER BY CreatedAtUtc DESC, Id DESC;
            SELECT @LastEmailAtUtc = MAX(CreatedAtUtc) FROM dbo.InterviewerCoachingSession
            WHERE InterviewerUserId = @UserId AND Channel = ''Email'';',
            N'@UserId INT, @FromUtc DATETIME, @ToUtc DATETIME, @Sessions INT OUTPUT, @LastCoachedAtUtc DATETIME OUTPUT,
              @LastOutcome VARCHAR(30) OUTPUT, @LastCoach VARCHAR(150) OUTPUT, @LastEmailAtUtc DATETIME OUTPUT',
            @UserId = @UserId, @FromUtc = @FromUtc, @ToUtc = @ToUtc, @Sessions = @Sessions OUTPUT,
            @LastCoachedAtUtc = @LastCoachedAtUtc OUTPUT, @LastOutcome = @LastOutcome OUTPUT,
            @LastCoach = @LastCoach OUTPUT, @LastEmailAtUtc = @LastEmailAtUtc OUTPUT;

    SELECT
        ul.id AS InterviewerUserId,
        ul.EmailId AS InterviewerEmail,
        NULLIF(LTRIM(RTRIM(ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, ''))), '') AS InterviewerName,
        NULLIF(LTRIM(RTRIM(ul.FirstName)), '') AS FirstName,
        NULLIF(NULLIF(LTRIM(RTRIM(CAST(ul.MobileNumber AS NVARCHAR(200)))), N''), N'0') AS Mobile, -- text in production ('+91...'): no numeric comparison
        CAST(ISNULL(ul.IsActive, 0) AS BIT) AS IsActive,
        ISNULL(@Sessions, 0) AS CoachingSessionsInRange,
        DATEADD(MINUTE, 330, @LastCoachedAtUtc) AS LastCoachedAtIst,
        @LastOutcome AS LastCoachingOutcome,
        @LastCoach AS LastCoachEmail,
        @LastEmailAtUtc AS LastSummaryEmailAtUtc
    FROM dbo.UserList ul
    WHERE ul.id = @UserId;

    /* Interviews by this interviewer in the range. */
    CREATE TABLE #Iv (InterviewId INT NOT NULL PRIMARY KEY, IrfId INT NULL, IrfName VARCHAR(200) NULL, InterviewDateIst DATETIME NULL);
    IF @UserId IS NOT NULL
        INSERT INTO #Iv (InterviewId, IrfId, IrfName, InterviewDateIst)
        SELECT im.id, irf.id, im.IRFId, MAX(im.interviewDate)
        FROM dbo.InterviewMeetings im
        OUTER APPLY (SELECT TOP 1 i.id FROM dbo.IRF i WHERE i.IRFId = im.IRFId ORDER BY i.id DESC) irf
        WHERE im.interviewer = @InterviewerEmail
          AND im.interviewDate >= @FromIstDt
          AND im.interviewDate <  @ToIstDt
        GROUP BY im.id, irf.id, im.IRFId;

    /* ---------- 2 Risk checks ---------- */
    SELECT iv.InterviewId, iv.IrfId, iv.IrfName, iv.InterviewDateIst,
           rc.SeverityBreakdown, rc.[Description], rc.Severity
    FROM #Iv iv
    INNER JOIN dbo.RiskCategory rc ON rc.InterviewId = iv.InterviewId
    WHERE NULLIF(LTRIM(RTRIM(ISNULL(rc.SeverityBreakdown, ''))), '') IS NOT NULL
       OR NULLIF(LTRIM(RTRIM(ISNULL(rc.[Description], ''))), '') IS NOT NULL
    ORDER BY iv.InterviewDateIst DESC;

    /* ---------- 3 Payment SLA deductions ---------- */
    SELECT iv.InterviewId, iv.IrfId, iv.IrfName, iv.InterviewDateIst,
           ps.PaymentDeduction, ps.Recommendation,
           COALESCE(NULLIF(LTRIM(RTRIM(ps.ModifiedBy)), ''), LTRIM(RTRIM(ps.CreatedBy))) AS LastSavedBy,
           ps.Inaccuratedecisionmaking, ps.TimeManagement, ps.QuestionsRelevance, ps.ScoreRubricIgnorance,
           ps.PoorFeedbackComments, ps.CandidateInteractionQuality, ps.LimitedHandoncoding, ps.WrongStatusUpdates,
           ps.WeakInfrastructure, ps.ProctoringIgnorance, ps.UnethicalPractices, ps.PoorCommunicationSkills,
           ps.AttendanceJoinCompliance, ps.InterviewerVideo
    FROM #Iv iv
    INNER JOIN dbo.PaymentSLADetails ps ON ps.Interviewid = iv.InterviewId
    WHERE ps.PaymentDeduction > 0
    ORDER BY iv.InterviewDateIst DESC;

    /* ---------- 4 Trial flags ---------- */
    IF OBJECT_ID(N'dbo.TrialFlags', N'U') IS NOT NULL AND @InterviewerId IS NOT NULL
        EXEC sp_executesql N'
            SELECT tf.Id AS TrialFlagId, tf.IrfId, irf.IRFId AS IrfName, tf.TriggeringInterviewId, tf.TriggeringInterviewDate,
                   tf.TranscriptQualityPass, tf.ReportQualityPass, tf.DecisionQualityPass, tf.CandidateRatingPass,
                   tf.JoinTimePass, tf.InterviewDurationPass, tf.TranscriptQualityScore, tf.CandidateRating,
                   tf.JoinTimeMinutes, tf.InterviewDurationMinutes, tf.OverallVerdict, LOWER(LTRIM(RTRIM(tf.ReviewStatus))) AS ReviewStatus
            FROM dbo.TrialFlags tf
            LEFT JOIN dbo.IRF irf ON irf.id = tf.IrfId
            WHERE tf.InterviewerId = @InterviewerId
              AND ((tf.FlaggedAtUtc >= @FromUtc AND tf.FlaggedAtUtc < @ToUtc)
                   OR LOWER(LTRIM(RTRIM(tf.ReviewStatus))) = ''pending'')
            ORDER BY tf.TriggeringInterviewDate DESC',
            N'@InterviewerId INT, @FromUtc DATETIME, @ToUtc DATETIME',
            @InterviewerId = @InterviewerId, @FromUtc = @FromUtc, @ToUtc = @ToUtc;
    ELSE
        SELECT CAST(NULL AS INT) AS TrialFlagId, CAST(NULL AS INT) AS IrfId, CAST(NULL AS VARCHAR(200)) AS IrfName,
               CAST(NULL AS INT) AS TriggeringInterviewId, CAST(NULL AS DATETIME) AS TriggeringInterviewDate,
               CAST(NULL AS BIT) AS TranscriptQualityPass, CAST(NULL AS BIT) AS ReportQualityPass, CAST(NULL AS BIT) AS DecisionQualityPass,
               CAST(NULL AS BIT) AS CandidateRatingPass, CAST(NULL AS BIT) AS JoinTimePass, CAST(NULL AS BIT) AS InterviewDurationPass,
               CAST(NULL AS DECIMAL(5, 2)) AS TranscriptQualityScore, CAST(NULL AS DECIMAL(5, 2)) AS CandidateRating,
               CAST(NULL AS DECIMAL(8, 2)) AS JoinTimeMinutes, CAST(NULL AS DECIMAL(8, 2)) AS InterviewDurationMinutes,
               CAST(NULL AS VARCHAR(30)) AS OverallVerdict, CAST(NULL AS VARCHAR(20)) AS ReviewStatus
        WHERE 1 = 0;

    /* ---------- 5 Channel standing (internal) ---------- */
    CREATE TABLE #St (Kind VARCHAR(20) NOT NULL, IrfId INT NULL, IrfName VARCHAR(200) NULL,
                      Detail NVARCHAR(500) NULL, IsOpen BIT NOT NULL, AtIst DATETIME NULL, InterviewId INT NULL);

    IF @UserId IS NOT NULL
    BEGIN
        IF COL_LENGTH('dbo.TransInterviewer', 'AutoQcVerdict') IS NOT NULL
            EXEC sp_executesql N'
                INSERT INTO #St (Kind, IrfId, IrfName, Detail, IsOpen, InterviewId)
                SELECT ''Audit'', t.IRFId, irf.IRFId, LEFT(N''Auto-QC verdict awaiting review: '' + t.AutoQcVerdict, 500), 1, t.AutoQcTriggeringInterviewId
                FROM dbo.TransInterviewer t
                LEFT JOIN dbo.IRF irf ON irf.id = t.IRFId
                WHERE t.UserId = @UserId AND t.IsActive = 1
                  AND NULLIF(LTRIM(RTRIM(ISNULL(t.AutoQcVerdict, ''''))), '''') IS NOT NULL',
                N'@UserId INT', @UserId = @UserId;

        IF OBJECT_ID(N'dbo.TransInterviewerAuditLog', N'U') IS NOT NULL
            EXEC sp_executesql N'
                INSERT INTO #St (Kind, IrfId, IrfName, Detail, IsOpen, AtIst, InterviewId)
                SELECT ''Audit'', t.IRFId, irf.IRFId,
                       LEFT(CASE WHEN l.EventType = ''RiskAuditRemoval'' THEN N''Removed by IRF audit'' ELSE N''Removed by auto-QC'' END
                            + ISNULL(N'': '' + NULLIF(l.ReasonCategory, N''''), N''''), 500),
                       0, DATEADD(MINUTE, 330, l.ChangedAt), l.TriggeringInterviewId
                FROM dbo.TransInterviewerAuditLog l
                INNER JOIN dbo.TransInterviewer t ON t.id = l.TransInterviewerId
                LEFT JOIN dbo.IRF irf ON irf.id = t.IRFId
                WHERE t.UserId = @UserId
                  AND l.EventType IN (''RiskAuditRemoval'', ''AutoQcRemoval'')
                  AND l.ChangedAt >= @FromUtc AND l.ChangedAt < @ToUtc',
                N'@UserId INT, @FromUtc DATETIME, @ToUtc DATETIME', @UserId = @UserId, @FromUtc = @FromUtc, @ToUtc = @ToUtc;

        INSERT INTO #St (Kind, IrfId, IrfName, Detail, IsOpen)
        SELECT 'RedMarker', trm.IRFId, irf.IRFId,
               LEFT(CAST(trm.RedMarker AS NVARCHAR(10)) + N' red marker' + CASE WHEN trm.RedMarker = 1 THEN N'' ELSE N's' END
                    + CASE ISNULL(trm.RedMarkerStatus, 0) WHEN 1 THEN N', under review' WHEN 2 THEN N', reviewed'
                                                         WHEN 3 THEN N', removed' ELSE N', not reviewed yet' END, 500),
               CAST(CASE WHEN ISNULL(trm.RedMarkerStatus, 0) NOT IN (2, 3) AND ISNULL(t.IsActive, 0) = 1 THEN 1 ELSE 0 END AS BIT)
        FROM dbo.TransInterviewerRedMarker trm
        LEFT JOIN dbo.IRF irf ON irf.id = trm.IRFId
        OUTER APPLY (SELECT TOP 1 x.IsActive FROM dbo.TransInterviewer x WHERE x.IRFId = trm.IRFId AND x.UserId = @UserId ORDER BY x.id DESC) t
        WHERE trm.InterviewerId = @ProfileId AND trm.RedMarker > 0;

        INSERT INTO #St (Kind, Detail, IsOpen)
        SELECT 'TotalInterviews',
               CASE ip.RedMarkerStatus WHEN 1 THEN N'Total Interviews: under review'
                                       WHEN 2 THEN N'Total Interviews: reviewed'
                                       ELSE N'Total Interviews: removed' END,
               CAST(CASE WHEN ip.RedMarkerStatus = 1 THEN 1 ELSE 0 END AS BIT)
        FROM dbo.InterviewerProfile ip
        WHERE ip.Id = @ProfileId AND ip.RedMarkerStatus IN (1, 2, 3);
    END

    SELECT Kind, IrfId, IrfName, Detail, IsOpen, AtIst, InterviewId
    FROM #St
    ORDER BY IsOpen DESC, Kind, IrfName;
END
GO
