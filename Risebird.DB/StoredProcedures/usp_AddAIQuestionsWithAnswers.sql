IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddAIQuestionsWithAnswers'))
BEGIN
    DROP PROCEDURE dbo.usp_AddAIQuestionsWithAnswers
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_AddAIQuestionsWithAnswers]
(
    @AIQuestionAnswersBulkData AS [dbo].[AddAIQuestionAnswers_TableType] READONLY,
    @IRFId int
)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRAN

        INSERT INTO dbo.SkillChannelSampleQuestions
            ([IRFId], [Skill], [Comment], [CreatedBy], [SkillName], [IdealAnswer], [ExpectedAnswer], [IsAIGenerated], [QuestionGist])
        SELECT
            t.IRFId,
            ISNULL(t.Skill, 0),
            t.Comment,
            t.CreatedBy,
            t.SkillName,
            t.IdealAnswer,
            t.IdealAnswer,
            1,
            t.Comment
        FROM @AIQuestionAnswersBulkData t
        WHERE t.IRFId = @IRFId
          AND t.Comment IS NOT NULL
          AND LTRIM(RTRIM(t.Comment)) <> ''
          AND NOT EXISTS (
              SELECT 1
              FROM dbo.SkillChannelSampleQuestions existing
              WHERE existing.IRFId = t.IRFId
                AND existing.Comment = t.Comment
          );

        SELECT @@ROWCOUNT AS InsertedCount;

        COMMIT TRAN
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        DECLARE @Message varchar(MAX) = ERROR_MESSAGE(),
            @Severity int = ERROR_SEVERITY(),
            @State smallint = ERROR_STATE()
        RAISERROR (@Message, @Severity, @State)
    END CATCH
END
GO
