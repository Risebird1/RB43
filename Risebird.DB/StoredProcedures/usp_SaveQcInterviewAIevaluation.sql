IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveQcInterviewAIevaluation'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveQcInterviewAIevaluation
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Saves transcript AI or report AI evaluation into QcInterviewAIevaluation (header) and QcInterviewAIevaluationDetails (red-flag rows).
-- One row per interview: TranscritAiDetails and ReportAIDetails can both be filled (transcript vs report). QCDone = 0 (not done).
-- Details: for each row, update if exists (same RedmarkedQCId, FlagName, SourceType) else insert; no delete.
-- @transcritAi: 1 = transcript AI (use @transcritAiDetails), 0 = report AI (use @reportAIDetails).
-- @redFlagsJson: JSON array of objects with FlagName, Verdict, and Remark (observation).
CREATE PROCEDURE [dbo].[usp_SaveQcInterviewAIevaluation]
    @interviewId INT,
    @score INT,
    @verdict VARCHAR(20),
    @transcritAi BIT = 1,
    @transcritAiDetails NVARCHAR(MAX) = NULL,
    @reportAIDetails NVARCHAR(MAX) = NULL,
    @redFlagsJson NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @qcId INT;
    DECLARE @sourceType VARCHAR(20) = CASE WHEN @transcritAi = 1 THEN 'TranscritAi' ELSE 'ReportAI' END;

    -- One row per interview: find existing by InterviewId
    SELECT @qcId = id
    FROM [dbo].[QcInterviewAIevaluation]
    WHERE [InterviewId] = @interviewId;

    IF @qcId IS NOT NULL
    BEGIN
        -- Update existing row: set only the column for this type (TranscritAi or ReportAI), keep the other. QCDone = 0.
        UPDATE [dbo].[QcInterviewAIevaluation]
        SET [QCDone] = 0,
            [QCDoneOn] = NULL,
            [ReportAI] = CASE WHEN @transcritAi = 0 THEN 1 ELSE [ReportAI] END,
            [TranscritAi] = CASE WHEN @transcritAi = 1 THEN 1 ELSE [TranscritAi] END,
            [ReportAIDetails] = CASE WHEN @transcritAi = 0 THEN @reportAIDetails ELSE [ReportAIDetails] END,
            [TranscritAiDetails] = CASE WHEN @transcritAi = 1 THEN @transcritAiDetails ELSE [TranscritAiDetails] END
        WHERE [id] = @qcId;
    END
    ELSE
    BEGIN
        -- Insert new header row; QCDone = 0. Set the details column for this type only.
        INSERT INTO [dbo].[QcInterviewAIevaluation] (
            [InterviewId],
            [ReportAI],
            [TranscritAi],
            [PenalityApplied],
            [QCDone],
            [QCDoneOn],
            [ReportAIDetails],
            [TranscritAiDetails]
        )
        VALUES (
            @interviewId,
            CASE WHEN @transcritAi = 0 THEN 1 ELSE 0 END,
            CASE WHEN @transcritAi = 1 THEN 1 ELSE 0 END,
            0,
            0,  -- QCDone (not done)
            NULL,
            CASE WHEN @transcritAi = 0 THEN @reportAIDetails ELSE NULL END,
            CASE WHEN @transcritAi = 1 THEN @transcritAiDetails ELSE NULL END
        );

        SET @qcId = SCOPE_IDENTITY();
    END

    -- For each detail row: update if exists (same RedmarkedQCId, FlagName, SourceType), else insert. No delete.
    IF @redFlagsJson IS NOT NULL AND LTRIM(RTRIM(@redFlagsJson)) != '' AND ISJSON(@redFlagsJson) = 1
    BEGIN
        MERGE [dbo].[QcInterviewAIevaluationDetails] AS target
        USING (
            SELECT
                @qcId AS RedmarkedQCId,
                @interviewId AS Interviewid,
                NULLIF(LTRIM(RTRIM(f.FlagName)), '') AS FlagName,
                NULLIF(LTRIM(RTRIM(f.Verdict)), '') AS Verdict,
                NULLIF(LTRIM(RTRIM(f.Remark)), '') AS Remark
            FROM OPENJSON(@redFlagsJson)
            WITH (
                FlagName VARCHAR(200) '$.FlagName',
                Verdict VARCHAR(20) '$.Verdict',
                Remark VARCHAR(500) '$.Remark'
            ) AS f
            WHERE f.FlagName IS NOT NULL
        ) AS source
        ON target.[RedmarkedQCId] = source.RedmarkedQCId
           AND target.[FlagName] = source.FlagName
           AND target.[SourceType] = @sourceType
        WHEN MATCHED THEN
            UPDATE SET
                target.[verdict] = source.Verdict,
                target.[Remark] = source.Remark
        WHEN NOT MATCHED BY TARGET THEN
            INSERT ([RedmarkedQCId], [Interviewid], [FlagName], [verdict], [Remark], [SourceType])
            VALUES (source.RedmarkedQCId, source.Interviewid, source.FlagName, source.Verdict, source.Remark, @sourceType);
    END
END
GO
