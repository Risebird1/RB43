CREATE OR ALTER PROCEDURE [dbo].[usp_GetTrialJudgmentContext]
    @InterviewId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'dbo.TrialFlags', N'U') IS NULL
    BEGIN
        SELECT
            CAST(0 AS BIT) AS IsEligible,
            CAST(0 AS BIT) AS IsFirstCompletedInterview,
            CAST(0 AS BIT) AS AlreadyFlagged,
            CAST(0 AS INT) AS ChannelId,
            CAST(0 AS INT) AS InterviewerId,
            CAST(0 AS INT) AS IrfId,
            CAST(NULL AS DATETIME) AS TriggeringInterviewDate,
            CAST(NULL AS DECIMAL(5,2)) AS TranscriptQualityScore,
            CAST(NULL AS DECIMAL(5,2)) AS ReportQualityScore,
            CAST(NULL AS DECIMAL(5,2)) AS CandidateRating,
            CAST(NULL AS DECIMAL(8,2)) AS JoinTimeMinutes,
            CAST(NULL AS DECIMAL(8,2)) AS InterviewDurationMinutes,
            CAST(NULL AS INT) AS RequiredDurationMinutes
        WHERE 1 = 0;
        RETURN;
    END

    DECLARE @IrfName VARCHAR(50);
    DECLARE @InterviewerRaw VARCHAR(200);
    DECLARE @InterviewDate DATETIME;
    DECLARE @FirstInterviewId INT;
    DECLARE @ChannelId INT;
    DECLARE @InterviewerId INT;
    DECLARE @IrfId INT;

    SELECT
        @IrfName = im.IRFId,
        @InterviewerRaw = LTRIM(RTRIM(im.interviewer)),
        @InterviewDate = im.interviewDate
    FROM dbo.InterviewMeetings im
    WHERE im.id = @InterviewId;

    IF @IrfName IS NULL OR @InterviewerRaw IS NULL OR @InterviewerRaw = ''
    BEGIN
        SELECT
            CAST(0 AS BIT) AS IsEligible,
            CAST(0 AS BIT) AS IsFirstCompletedInterview,
            CAST(0 AS BIT) AS AlreadyFlagged,
            CAST(0 AS INT) AS ChannelId,
            CAST(0 AS INT) AS InterviewerId,
            CAST(0 AS INT) AS IrfId,
            CAST(NULL AS DATETIME) AS TriggeringInterviewDate,
            CAST(NULL AS DECIMAL(5,2)) AS TranscriptQualityScore,
            CAST(NULL AS DECIMAL(5,2)) AS ReportQualityScore,
            CAST(NULL AS DECIMAL(5,2)) AS CandidateRating,
            CAST(NULL AS DECIMAL(8,2)) AS JoinTimeMinutes,
            CAST(NULL AS DECIMAL(8,2)) AS InterviewDurationMinutes,
            CAST(NULL AS INT) AS RequiredDurationMinutes;
        RETURN;
    END

    ;WITH CompletedOnIrf AS (
        SELECT
            im.id,
            im.interviewDate,
            ROW_NUMBER() OVER (ORDER BY im.interviewDate ASC, im.id ASC) AS rn
        FROM dbo.InterviewMeetings im
        INNER JOIN dbo.InterviewComments ic ON ic.InterviewId = im.id
        WHERE im.IRFId = @IrfName
          AND LTRIM(RTRIM(im.interviewer)) = @InterviewerRaw
          AND ISNULL(im.confirmationStatus, 0) = 1
          AND (
                ic.MeetingStatus IN (4, 6, 7, 8)
             OR ic.InterviewStatus IN (2, 3, 4, 5)
          )
    )
    SELECT @FirstInterviewId = id
    FROM CompletedOnIrf
    WHERE rn = 1;

    -- Same TransInterviewer resolution as usp_GetTransInterviewerContextForInterview (auto-QC path).
    SELECT TOP 1
        @IrfId = ti.IRFId,
        @ChannelId = ti.id,
        @InterviewerId = ti.InterviewerId
    FROM dbo.InterviewMeetings im
    INNER JOIN dbo.IRF irf ON irf.IRFId = im.IRFId
    CROSS APPLY (
        SELECT TOP 1 u.UserId
        FROM (
            SELECT ul.id AS UserId, 1 AS Priority
            FROM dbo.UserList ul
            WHERE LTRIM(RTRIM(ul.EmailId)) = @InterviewerRaw
            UNION ALL
            SELECT ul.id, 2
            FROM dbo.UserList ul
            WHERE ul.EmailId = im.interviewer
            UNION ALL
            SELECT ip.UserId, 3
            FROM dbo.InterviewerProfile ip
            INNER JOIN dbo.UserList ul ON ul.id = ip.UserId
            WHERE LTRIM(RTRIM(ISNULL(ip.NickName, ''))) = @InterviewerRaw
        ) u
        ORDER BY u.Priority
    ) ru
    INNER JOIN dbo.TransInterviewer ti ON ti.IRFId = irf.id
       AND (
            ti.UserId = ru.UserId
            OR EXISTS (
                SELECT 1
                FROM dbo.Interviewer iv
                WHERE iv.id = ti.InterviewerId
                  AND iv.UserId = ru.UserId
            )
            OR EXISTS (
                SELECT 1
                FROM dbo.InterviewerProfile ipr
                WHERE ipr.id = ti.InterviewerId
                  AND ipr.UserId = ru.UserId
            )
       )
    WHERE im.id = @InterviewId
    ORDER BY ti.id;

    DECLARE @IsFirst BIT = CASE WHEN @FirstInterviewId = @InterviewId THEN 1 ELSE 0 END;
    DECLARE @AlreadyFlagged BIT = CASE
        WHEN @InterviewerId IS NOT NULL AND @IrfId IS NOT NULL
             AND EXISTS (SELECT 1 FROM dbo.TrialFlags tf WHERE tf.InterviewerId = @InterviewerId AND tf.IrfId = @IrfId)
        THEN 1 ELSE 0 END;

    DECLARE @JoinedOn DATETIME;
    SELECT @JoinedOn = MIN(j.Joinedon)
    FROM dbo.InterviewMeetingsJoinTime j
    INNER JOIN dbo.InterviewMeetings im ON im.id = j.Interviewid
    WHERE j.Interviewid = @InterviewId
      AND LTRIM(RTRIM(j.interviewer)) = LTRIM(RTRIM(im.interviewer));

    DECLARE @ActualDuration INT;
    DECLARE @RequiredDuration INT;
    SELECT
        @ActualDuration = ic.VideoDuration,
        @RequiredDuration = pt.DurationSelectedMin
    FROM dbo.InterviewMeetings im
    INNER JOIN dbo.InterviewComments ic ON ic.InterviewId = im.id
    INNER JOIN dbo.IRF irf ON irf.IRFId = im.IRFId
    LEFT JOIN dbo.PlanTypeChargesMaster pt
        ON pt.CountryType = irf.JobCountry
       AND pt.PlanTypeEnumValue = irf.Heuristicsscore
       AND irf.CompanyType = pt.CompanyType
    WHERE im.id = @InterviewId;

    DECLARE @TranscriptScore DECIMAL(5,2);
    DECLARE @ReportScore DECIMAL(5,2);
    DECLARE @CandidateRating DECIMAL(5,2);

    SELECT
        @TranscriptScore = CAST(NULLIF(ic.AITranscriptAnalysisScore, 0) AS DECIMAL(5,2)),
        @ReportScore = CAST(NULLIF(ic.AIReportQCAnalysisScore, 0) AS DECIMAL(5,2))
    FROM dbo.InterviewComments ic
    WHERE ic.InterviewId = @InterviewId;

    SELECT TOP 1 @CandidateRating = CAST(cfd.Rating AS DECIMAL(5,2))
    FROM dbo.CandidateFeedbackDetails cfd
    WHERE cfd.Interviewid = @InterviewId
      AND cfd.Rating IS NOT NULL
      AND cfd.Rating > 0
    ORDER BY cfd.Id DESC;

    DECLARE @JoinMinutes DECIMAL(8,2) = CASE
        WHEN @JoinedOn IS NULL OR @InterviewDate IS NULL THEN NULL
        WHEN DATEDIFF(MINUTE, @InterviewDate, @JoinedOn) < 0 THEN 0
        ELSE CAST(DATEDIFF(MINUTE, @InterviewDate, @JoinedOn) AS DECIMAL(8,2))
    END;

    SELECT
        CAST(CASE WHEN @IsFirst = 1 AND @AlreadyFlagged = 0 AND @ChannelId IS NOT NULL AND @IrfId IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS IsEligible,
        @IsFirst AS IsFirstCompletedInterview,
        @AlreadyFlagged AS AlreadyFlagged,
        ISNULL(@ChannelId, 0) AS ChannelId,
        ISNULL(@InterviewerId, 0) AS InterviewerId,
        ISNULL(@IrfId, 0) AS IrfId,
        @InterviewDate AS TriggeringInterviewDate,
        @TranscriptScore AS TranscriptQualityScore,
        @ReportScore AS ReportQualityScore,
        @CandidateRating AS CandidateRating,
        @JoinMinutes AS JoinTimeMinutes,
        CAST(NULLIF(@ActualDuration, 0) AS DECIMAL(8,2)) AS InterviewDurationMinutes,
        NULLIF(@RequiredDuration, 0) AS RequiredDurationMinutes;
END
GO
