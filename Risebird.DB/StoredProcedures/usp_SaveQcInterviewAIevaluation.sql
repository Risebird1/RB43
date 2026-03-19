IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveQcInterviewAIevaluation'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveQcInterviewAIevaluation
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Saves transcript AI evaluation into QcInterviewAIevaluation (header) and QcInterviewAIevaluationDetails (red-flag rows).
-- If an entry already exists for this InterviewId + TranscritAi, updates it; otherwise inserts new. QCDone = 0 (not done).
-- Details: for each row, update if exists (same RedmarkedQCId, FlagName, SourceType) else insert; no delete.
-- @transcritAi: 1 = transcript AI evaluation, 0 = report AI (or other). Used to find/update the correct row.
-- @transcritAiDetails: Full JSON response to store in TranscritAiDetails.
-- @redFlagsJson: JSON array of objects with FlagName, Verdict, and Remark (observation).
CREATE PROCEDURE [dbo].[usp_SaveQcInterviewAIevaluation]
    @interviewId INT,
    @score INT,
    @verdict VARCHAR(20),
    @transcritAi BIT = 1,
    @transcritAiDetails NVARCHAR(MAX) = NULL,
    @redFlagsJson NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @qcId INT;
    DECLARE @sourceType VARCHAR(20) = CASE WHEN @transcritAi = 1 THEN 'TranscritAi' ELSE 'ReportAI' END;

    -- Check for existing row for this interview and TranscritAi value
    SELECT @qcId = id
    FROM [dbo].[QcInterviewAIevaluation]
    WHERE [InterviewId] = @interviewId

    IF @qcId IS NOT NULL
    BEGIN
        -- Update existing row (no duplicate); QCDone = 0 (not done). Do not delete details.
        UPDATE [dbo].[QcInterviewAIevaluation]
        SET  [QCDone] = 0,
            [QCDoneOn] = NULL,
            [TranscritAi] = @transcritAi,
            [TranscritAiDetails] = @transcritAiDetails
        WHERE [id] = @qcId;
    END
    ELSE
    BEGIN
        -- Insert new header row; QCDone = 0 (not done)
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
            CASE WHEN @transcritAi = 1 THEN 0 ELSE 1 END,  -- ReportAI
            @transcritAi,
            0,
            0,  -- QCDone (not done)
            NULL,  -- QCDoneOn
            NULL,
            @transcritAiDetails
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
