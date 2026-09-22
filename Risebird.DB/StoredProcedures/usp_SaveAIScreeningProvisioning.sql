IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveAIScreeningProvisioning'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveAIScreeningProvisioning
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
-- Stamps the AI-screening-specific fields onto a channel that
-- usp_CreateSkillChannelFromTemplateV1 has just created.
--
-- V1 is shared with the human-panel route and is deliberately left unmodified, but it
-- does not write SkillSetsevaluate, the stored-prompt ids or the ideal candidate profile,
-- and it copies the rubric into TransTechSkill without SkillSet. This proc finishes the job:
--
--   SkillSetsevaluate = 2        -- the interview always has exactly two scored sections
--   OpenAIPromptId               -- Primary Prompt Id, runs the interview (mandatory for an AI channel)
--   OpenAIEvaluatorPromptId      -- Interview Evaluator Prompt Id (optional; primary evaluates when null)
--   IdealCandidateProfile        -- the profile the recruiter confirmed at intake step 2
--   TransTechSkill               -- replaced with the sectioned rubric (SkillSet 1 and 2)
--
-- Re-running it for the same channel is safe: every write is an overwrite.
Create PROCEDURE [dbo].[usp_SaveAIScreeningProvisioning]
@IRFId int,
@SkillSetsevaluate int = 2,
@OpenAIPromptId varchar(100) = null,
@OpenAIEvaluatorPromptId varchar(100) = null,
@IdealCandidateProfile varchar(max) = null,
@TechSkillBulkData AS [dbo].[TransTechSkill_TableTypeV5] READONLY
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRAN

        UPDATE IRF
        SET [SkillSetsevaluate]       = @SkillSetsevaluate
           ,[OpenAIPromptId]          = @OpenAIPromptId
           ,[OpenAIEvaluatorPromptId] = @OpenAIEvaluatorPromptId
           ,[IdealCandidateProfile]   = @IdealCandidateProfile
        WHERE id = @IRFId;

        -- Replace the flat rubric V1 copied from the template with the sectioned one.
        IF EXISTS (SELECT 1 FROM @TechSkillBulkData)
        BEGIN
            DELETE FROM TransTechSkill WHERE IRFId = @IRFId;

            INSERT INTO [dbo].[TransTechSkill]
                ([IRFId]
                ,[LevelId]
                ,[SkillId]
                ,[Weightage]
                ,[Concepts]
                ,[Natureofquestion]
                ,[Timelimit]
                ,[isEliminationskill]
                ,[SkillName]
                ,[SkillSet])
            SELECT @IRFId
                ,tsbd.LevelId
                ,tsbd.SkillId
                ,tsbd.Weightage
                ,tsbd.Concepts
                ,tsbd.Natureofquestion
                ,tsbd.Timelimit
                ,tsbd.isEliminationskill
                ,tsbd.SkillName
                ,tsbd.SkillSet
            FROM @TechSkillBulkData tsbd;
        END

        COMMIT TRAN
    END TRY
    BEGIN CATCH
        IF (@@TRANCOUNT > 0)
            ROLLBACK TRAN

        THROW;
    END CATCH
END
GO
