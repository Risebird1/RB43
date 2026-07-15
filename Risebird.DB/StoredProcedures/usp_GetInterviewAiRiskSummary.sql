CREATE OR ALTER PROCEDURE [dbo].[usp_GetInterviewAiRiskSummary]
    @InterviewId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ReportScoreLt60_P2_Max INT = 60;
    DECLARE @ReportScoreLt60_P1_Max INT = 40;
    DECLARE @ReportScoreLt60_P0_Max INT = 20;
    DECLARE @TranscriptLt4_P2_Min INT = 3;
    DECLARE @TranscriptLt4_P1_Min INT = 2;
    DECLARE @TranscriptLt6_P4_Min INT = 5;
    DECLARE @TranscriptLt6_P3_Min INT = 4;
    DECLARE @TranscriptLt6_P2_Min INT = 3;
    DECLARE @TranscriptLt6_P1_Min INT = 2;
    DECLARE @Contradiction_MaxScore INT = 5;

    DECLARE @HasMergedJson BIT = CASE
        WHEN COL_LENGTH('dbo.QcInterviewAIevaluation', 'MergedAIQCDetails') IS NOT NULL THEN 1 ELSE 0 END;

    DECLARE @ReportScore DECIMAL(10,2) = NULL;
    DECLARE @TranscriptScore DECIMAL(10,2) = NULL;
    DECLARE @ContradictionScore DECIMAL(10,2) = NULL;
    DECLARE @RedoBit BIT = 0;
    DECLARE @Suspicious VARCHAR(20) = NULL;

    SELECT
        @ReportScore = CAST(NULLIF(ic.AIReportQCAnalysisScore, 0) AS DECIMAL(10,2)),
        @TranscriptScore = CAST(NULLIF(ic.AITranscriptAnalysisScore, 0) AS DECIMAL(10,2))
    FROM dbo.InterviewComments ic
    WHERE ic.InterviewId = @InterviewId;

    SELECT
        @ContradictionScore = TRY_CAST(qca.ContradictionScore AS DECIMAL(10,2)),
        @RedoBit = CASE
            WHEN ISNULL(qca.AIMergeRedoInterview, 0) = 1 THEN 1
            WHEN @HasMergedJson = 1
             AND ISNULL(TRY_CAST(JSON_VALUE(qca.MergedAIQCDetails, '$.RedoInterview') AS BIT), 0) = 1 THEN 1
            ELSE 0 END,
        @Suspicious = COALESCE(
            NULLIF(LTRIM(RTRIM(qca.AIMergeInterviewerSuspicious)), ''),
            CASE WHEN @HasMergedJson = 1
                 THEN NULLIF(LTRIM(RTRIM(JSON_VALUE(qca.MergedAIQCDetails, '$.interviewer_suspicious'))), '')
                 ELSE NULL END
        )
    FROM dbo.QcInterviewAIevaluation qca
    WHERE qca.InterviewId = @InterviewId;

    -- Report AI (#7)
    SELECT
        CAST('Report AI' AS VARCHAR(40)) AS SourceName,
        CAST(@ReportScore AS VARCHAR(40)) AS RawScore,
        CAST(CASE WHEN @ReportScore IS NOT NULL AND @ReportScore < @ReportScoreLt60_P2_Max THEN 1 ELSE 0 END AS BIT) AS Triggered,
        CAST(CASE
            WHEN @ReportScore IS NULL OR @ReportScore >= @ReportScoreLt60_P2_Max THEN NULL
            WHEN @ReportScore <= @ReportScoreLt60_P0_Max THEN 'P0'
            WHEN @ReportScore <= @ReportScoreLt60_P1_Max THEN 'P1'
            ELSE 'P2'
        END AS VARCHAR(4)) AS PLevel

    UNION ALL
    -- Transcript AI (#2 and #8 → worst P of the two)
    SELECT
        'Transcript AI',
        CAST(@TranscriptScore AS VARCHAR(40)),
        CAST(CASE WHEN @TranscriptScore IS NOT NULL AND @TranscriptScore < 6 THEN 1 ELSE 0 END AS BIT),
        CAST(CASE
            WHEN @TranscriptScore IS NULL OR @TranscriptScore >= 6 THEN NULL
            WHEN @TranscriptScore < 4 THEN
                -- min of #2 and #8 for scores < 4
                CASE
                    WHEN @TranscriptScore < @TranscriptLt4_P1_Min THEN 'P0'  -- both point to 0
                    WHEN @TranscriptScore < @TranscriptLt4_P2_Min THEN 'P1'
                    ELSE 'P2'
                END
            WHEN @TranscriptScore < @TranscriptLt6_P1_Min THEN 'P0'
            WHEN @TranscriptScore < @TranscriptLt6_P2_Min THEN 'P1'
            WHEN @TranscriptScore < @TranscriptLt6_P3_Min THEN 'P2'
            WHEN @TranscriptScore < @TranscriptLt6_P4_Min THEN 'P3'
            ELSE 'P4'
        END AS VARCHAR(4))

    UNION ALL
    -- Contradiction AI (#9)
    SELECT
        'Contradiction AI',
        CAST(@ContradictionScore AS VARCHAR(40)),
        CAST(CASE
            WHEN @ContradictionScore IS NOT NULL
             AND @ContradictionScore BETWEEN 1 AND @Contradiction_MaxScore THEN 1 ELSE 0 END AS BIT),
        CAST(CASE CAST(@ContradictionScore AS INT)
            WHEN 1 THEN 'P0' WHEN 2 THEN 'P1' WHEN 3 THEN 'P2' WHEN 4 THEN 'P3' WHEN 5 THEN 'P4'
            ELSE NULL END AS VARCHAR(4))

    UNION ALL
    -- AI Merge (#6 redo)
    SELECT
        'AI Merge',
        CASE
            WHEN @RedoBit = 1 THEN 'RedoInterview=1'
            WHEN UPPER(ISNULL(@Suspicious, '')) IN ('YES', 'Y', 'TRUE') THEN 'interviewer_suspicious=' + @Suspicious
            ELSE 'OK'
        END,
        CAST(CASE WHEN @RedoBit = 1 THEN 1 ELSE 0 END AS BIT),
        CAST(CASE WHEN @RedoBit = 1 THEN 'P0' ELSE NULL END AS VARCHAR(4));
END
GO
