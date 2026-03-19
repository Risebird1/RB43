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
-- @transcritAiDetails: Full JSON response to store in TranscritAiDetails.
-- @redFlagsJson: JSON array of objects with FlagName, Verdict, and Remark (observation).
CREATE PROCEDURE [dbo].[usp_SaveQcInterviewAIevaluation]
    @interviewId INT,
    @score INT,
    @verdict VARCHAR(20),
    @transcritAiDetails NVARCHAR(MAX) = NULL,
    @redFlagsJson NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @qcId INT;

    -- Insert header row (transcript AI evaluation); store full JSON response in TranscritAiDetails
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
        0,  -- ReportAI
        1,  -- TranscritAi
        CASE WHEN ISNULL(@score, 10) < 10 THEN 10 - ISNULL(@score, 10) ELSE 0 END,
        1,  -- QCDone
        GETDATE(),
        NULL,
        @transcritAiDetails
    );

    SET @qcId = SCOPE_IDENTITY();

    -- Insert detail rows (red flags: parameter name + verdict only) from JSON
    IF @redFlagsJson IS NOT NULL AND LTRIM(RTRIM(@redFlagsJson)) != '' AND ISJSON(@redFlagsJson) = 1
    BEGIN
        INSERT INTO [dbo].[QcInterviewAIevaluationDetails] (
            [RedmarkedQCId],
            [Interviewid],
            [FlagName],
            [verdict],
            [Remark],
            [SourceType]
        )
        SELECT
            @qcId,
            @interviewId,
            NULLIF(LTRIM(RTRIM(f.FlagName)), ''),
            NULLIF(LTRIM(RTRIM(f.Verdict)), ''),
            NULLIF(LTRIM(RTRIM(f.Remark)), ''),
            'TranscritAi'
        FROM OPENJSON(@redFlagsJson)
        WITH (
            FlagName VARCHAR(200) '$.FlagName',
            Verdict VARCHAR(20) '$.Verdict',
            Remark VARCHAR(500) '$.Remark'
        ) AS f
        WHERE f.FlagName IS NOT NULL;
    END
END
GO
