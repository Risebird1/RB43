IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetQCOwnerFlagDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetQCOwnerFlagDetails
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns flag name, sub flag value, and count for a QC owner in the date range.
-- Used by QC Owner Dashboard popup when clicking Total Flags Reported.
CREATE PROCEDURE dbo.usp_GetQCOwnerFlagDetails
    @QCOwner VARCHAR(150),
    @DateFrom DATE = NULL,
    @DateTo   DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @DateFrom IS NULL
        SET @DateFrom = DATEADD(DAY, -30, CAST(GETDATE() AS DATE));
    IF @DateTo IS NULL
        SET @DateTo = CAST(GETDATE() AS DATE);

    ;WITH Unpivoted AS (
        SELECT LTRIM(RTRIM(ISNULL(psd.Inaccuratedecisionmaking,''))) AS SubFlagValue, 'Inaccurate Decision Making' AS FlagName FROM PaymentSLADetails psd INNER JOIN InterviewMeetings im ON im.id = psd.Interviewid
        WHERE psd.isQcDone = 1 AND psd.CreatedBy = @QCOwner AND im.Interviewdate >= @DateFrom AND im.Interviewdate < DATEADD(DAY, 1, @DateTo) AND LEN(LTRIM(RTRIM(ISNULL(psd.Inaccuratedecisionmaking,'')))) > 0
        UNION ALL
        SELECT LTRIM(RTRIM(ISNULL(psd.TimeManagement,''))), 'Time Management' FROM PaymentSLADetails psd INNER JOIN InterviewMeetings im ON im.id = psd.Interviewid
        WHERE psd.isQcDone = 1 AND psd.CreatedBy = @QCOwner AND im.Interviewdate >= @DateFrom AND im.Interviewdate < DATEADD(DAY, 1, @DateTo) AND LEN(LTRIM(RTRIM(ISNULL(psd.TimeManagement,'')))) > 0
        UNION ALL
        SELECT LTRIM(RTRIM(ISNULL(psd.QuestionsRelevance,''))), 'Questions Relevance' FROM PaymentSLADetails psd INNER JOIN InterviewMeetings im ON im.id = psd.Interviewid
        WHERE psd.isQcDone = 1 AND psd.CreatedBy = @QCOwner AND im.Interviewdate >= @DateFrom AND im.Interviewdate < DATEADD(DAY, 1, @DateTo) AND LEN(LTRIM(RTRIM(ISNULL(psd.QuestionsRelevance,'')))) > 0
        UNION ALL
        SELECT LTRIM(RTRIM(ISNULL(psd.ScoreRubricIgnorance,''))), 'Score Rubric Ignorance' FROM PaymentSLADetails psd INNER JOIN InterviewMeetings im ON im.id = psd.Interviewid
        WHERE psd.isQcDone = 1 AND psd.CreatedBy = @QCOwner AND im.Interviewdate >= @DateFrom AND im.Interviewdate < DATEADD(DAY, 1, @DateTo) AND LEN(LTRIM(RTRIM(ISNULL(psd.ScoreRubricIgnorance,'')))) > 0
        UNION ALL
        SELECT LTRIM(RTRIM(ISNULL(psd.PoorFeedbackComments,''))), 'Poor Feedback Comments' FROM PaymentSLADetails psd INNER JOIN InterviewMeetings im ON im.id = psd.Interviewid
        WHERE psd.isQcDone = 1 AND psd.CreatedBy = @QCOwner AND im.Interviewdate >= @DateFrom AND im.Interviewdate < DATEADD(DAY, 1, @DateTo) AND LEN(LTRIM(RTRIM(ISNULL(psd.PoorFeedbackComments,'')))) > 0
        UNION ALL
        SELECT LTRIM(RTRIM(ISNULL(psd.CandidateInteractionQuality,''))), 'Candidate Interaction Quality' FROM PaymentSLADetails psd INNER JOIN InterviewMeetings im ON im.id = psd.Interviewid
        WHERE psd.isQcDone = 1 AND psd.CreatedBy = @QCOwner AND im.Interviewdate >= @DateFrom AND im.Interviewdate < DATEADD(DAY, 1, @DateTo) AND LEN(LTRIM(RTRIM(ISNULL(psd.CandidateInteractionQuality,'')))) > 0
        UNION ALL
        SELECT LTRIM(RTRIM(ISNULL(psd.LimitedHandoncoding,''))), 'Limited Hands-on Coding' FROM PaymentSLADetails psd INNER JOIN InterviewMeetings im ON im.id = psd.Interviewid
        WHERE psd.isQcDone = 1 AND psd.CreatedBy = @QCOwner AND im.Interviewdate >= @DateFrom AND im.Interviewdate < DATEADD(DAY, 1, @DateTo) AND LEN(LTRIM(RTRIM(ISNULL(psd.LimitedHandoncoding,'')))) > 0
        UNION ALL
        SELECT LTRIM(RTRIM(ISNULL(psd.WrongStatusUpdates,''))), 'Wrong Status Updates' FROM PaymentSLADetails psd INNER JOIN InterviewMeetings im ON im.id = psd.Interviewid
        WHERE psd.isQcDone = 1 AND psd.CreatedBy = @QCOwner AND im.Interviewdate >= @DateFrom AND im.Interviewdate < DATEADD(DAY, 1, @DateTo) AND LEN(LTRIM(RTRIM(ISNULL(psd.WrongStatusUpdates,'')))) > 0
        UNION ALL
        SELECT LTRIM(RTRIM(ISNULL(psd.WeakInfrastructure,''))), 'Weak Infrastructure' FROM PaymentSLADetails psd INNER JOIN InterviewMeetings im ON im.id = psd.Interviewid
        WHERE psd.isQcDone = 1 AND psd.CreatedBy = @QCOwner AND im.Interviewdate >= @DateFrom AND im.Interviewdate < DATEADD(DAY, 1, @DateTo) AND LEN(LTRIM(RTRIM(ISNULL(psd.WeakInfrastructure,'')))) > 0
        UNION ALL
        SELECT LTRIM(RTRIM(ISNULL(psd.ProctoringIgnorance,''))), 'Proctoring Ignorance' FROM PaymentSLADetails psd INNER JOIN InterviewMeetings im ON im.id = psd.Interviewid
        WHERE psd.isQcDone = 1 AND psd.CreatedBy = @QCOwner AND im.Interviewdate >= @DateFrom AND im.Interviewdate < DATEADD(DAY, 1, @DateTo) AND LEN(LTRIM(RTRIM(ISNULL(psd.ProctoringIgnorance,'')))) > 0
        UNION ALL
        SELECT LTRIM(RTRIM(ISNULL(psd.UnethicalPractices,''))), 'Unethical Practices' FROM PaymentSLADetails psd INNER JOIN InterviewMeetings im ON im.id = psd.Interviewid
        WHERE psd.isQcDone = 1 AND psd.CreatedBy = @QCOwner AND im.Interviewdate >= @DateFrom AND im.Interviewdate < DATEADD(DAY, 1, @DateTo) AND LEN(LTRIM(RTRIM(ISNULL(psd.UnethicalPractices,'')))) > 0
        UNION ALL
        SELECT LTRIM(RTRIM(ISNULL(psd.PoorCommunicationSkills,''))), 'Poor Communication Skills' FROM PaymentSLADetails psd INNER JOIN InterviewMeetings im ON im.id = psd.Interviewid
        WHERE psd.isQcDone = 1 AND psd.CreatedBy = @QCOwner AND im.Interviewdate >= @DateFrom AND im.Interviewdate < DATEADD(DAY, 1, @DateTo) AND LEN(LTRIM(RTRIM(ISNULL(psd.PoorCommunicationSkills,'')))) > 0
    )
    SELECT
        FlagName,
        SubFlagValue,
        COUNT(*) AS FlagCount
    FROM Unpivoted
    GROUP BY FlagName, SubFlagValue
    ORDER BY FlagName, FlagCount DESC;
END
GO
