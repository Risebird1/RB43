IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetAIQuestionPoolForSets'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAIQuestionPoolForSets
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
-- Loads the two inputs set creation needs: the channel's rubric parameters (in rubric order)
-- and its question pool.
--
-- Schema drift: production's Delivery Manager carries per-question minutes, a max score and a
-- difficulty level. This database has only CodingTimeLimitInMinutes, so the optional columns are
-- detected at runtime and returned as NULL when absent (set totals then compute as 0). If a
-- deployment names them differently, change the COL_LENGTH checks in THIS PROCEDURE and nothing
-- else -- the C# and the save-time gate never read those columns.
Create PROCEDURE [dbo].[usp_GetAIQuestionPoolForSets]
	@IRFId int
AS
BEGIN
    SET NOCOUNT ON;

    -- A preset AI interview channel is IsAIInterview = 1 with AIInterviewType = 2 (Preset).
    -- Reported as its own result set so the caller can say precisely why creation is unavailable
    -- rather than inferring it from an empty pool.
    DECLARE @IsPreset bit =
        CASE WHEN EXISTS (SELECT 1 FROM IRF
                          WHERE id = @IRFId
                            AND ISNULL(IsAIInterview, 0) = 1
                            AND ISNULL(AIInterviewType, 0) = 2)
             THEN 1 ELSE 0 END;

    SELECT @IsPreset AS IsPreset;

    -- Anything that is not preset has no papers to deal, whoever the caller is.
    IF @IsPreset = 0
    BEGIN
        SELECT CAST(NULL AS nvarchar(500)) AS SkillName, CAST(NULL AS int) AS ParameterOrder WHERE 1 = 0;
        SELECT CAST(NULL AS int) AS id, CAST(NULL AS nvarchar(500)) AS SkillName,
               CAST(NULL AS nvarchar(max)) AS Title, CAST(NULL AS int) AS MaxTimeMinutes,
               CAST(NULL AS int) AS MaxScore, CAST(NULL AS nvarchar(100)) AS Difficulty WHERE 1 = 0;
        RETURN;
    END

    -- Rubric parameters are the distinct TransTechSkill.SkillName values for the channel.
    -- ParameterOrder fixes the ask order of questions inside every set.
    SELECT LTRIM(RTRIM(tts.SkillName)) AS SkillName,
           ROW_NUMBER() OVER (ORDER BY MIN(tts.id)) AS ParameterOrder
    FROM TransTechSkill tts
    WHERE tts.IRFId = @IRFId
      AND tts.SkillName IS NOT NULL
      AND LTRIM(RTRIM(tts.SkillName)) <> ''
    GROUP BY LTRIM(RTRIM(tts.SkillName))
    ORDER BY MIN(tts.id);

    DECLARE @MaxTimeMinutes nvarchar(200) = 'CAST(NULL AS int)';
    DECLARE @MaxScore nvarchar(200) = 'CAST(NULL AS int)';
    DECLARE @Difficulty nvarchar(200) = 'CAST(NULL AS nvarchar(100))';

    IF COL_LENGTH('dbo.SkillChannelSampleQuestions', 'MaxTimeMinutes') IS NOT NULL
        SET @MaxTimeMinutes = 'q.MaxTimeMinutes';
    ELSE IF COL_LENGTH('dbo.SkillChannelSampleQuestions', 'CodingTimeLimitInMinutes') IS NOT NULL
        SET @MaxTimeMinutes = 'q.CodingTimeLimitInMinutes';

    IF COL_LENGTH('dbo.SkillChannelSampleQuestions', 'MaxScore') IS NOT NULL
        SET @MaxScore = 'q.MaxScore';

    IF COL_LENGTH('dbo.SkillChannelSampleQuestions', 'Difficulty') IS NOT NULL
        SET @Difficulty = 'CAST(q.Difficulty AS nvarchar(100))';

    DECLARE @Sql nvarchar(max) = N'
    SELECT q.id,
           LTRIM(RTRIM(q.SkillName)) AS SkillName,
           q.Comment AS Title,
           ' + @MaxTimeMinutes + N' AS MaxTimeMinutes,
           ' + @MaxScore + N' AS MaxScore,
           ' + @Difficulty + N' AS Difficulty
    FROM SkillChannelSampleQuestions q
    WHERE q.IRFId = @IRFId
      AND q.SkillName IS NOT NULL
      AND LTRIM(RTRIM(q.SkillName)) <> ''''
      AND q.Comment IS NOT NULL
      AND LTRIM(RTRIM(q.Comment)) <> ''''
    ORDER BY q.id;';

    EXEC sp_executesql @Sql, N'@IRFId int', @IRFId = @IRFId;
END
GO
