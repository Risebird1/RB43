IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_CreateAIAudioTemplate'))
BEGIN
    DROP PROCEDURE dbo.usp_CreateAIAudioTemplate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
-- Creates the template behind an AI Audio Adaptive screening channel.
--
-- Modelled on usp_CreateTemplateFromMAG with the Assessment Group machinery removed:
--   no MAG gate            -- an arbitrary job description has no curated MAG row
--   no interviewer panel   -- usp_CreateSkillChannelFromTemplateV1 never reads
--                             TransClonableIRFInterviewer, and an AI channel has no human panel
--   no question bank       -- the adaptive assistant improvises from the rubric and the JD
--
-- @AssessmentGroupId is still stamped (with the real AG on an exact job-title match, otherwise
-- the catch-all) because ~19 procs INNER JOIN on it and would silently drop the channel.
--
-- Idempotent on @TemplateName: a double submit returns the existing template id rather than
-- violating UC_ClonableSkillChannel_Name.
Create PROCEDURE [dbo].[usp_CreateAIAudioTemplate]
@TemplateName varchar(200),
@AssessmentGroupId int,
@TechSkillBulkData AS [dbo].[TransTechSkill_TableTypeV5] READONLY,
@CreatedOn datetime,
@CreatedBy varchar(100) = null,
@MeetingPlatform varchar(25) = 'Zoho',
@IsAadhaarVerificationRequired bit = 0,
@ClonableIRFId int OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @ClonableIRFId = 0;

    BEGIN TRY
        BEGIN TRAN

        -- Double submit / retry: hand back the template already created for this draft.
        SELECT TOP 1 @ClonableIRFId = id
        FROM ClonableSkillChannel
        WHERE [Name] = @TemplateName
          AND (IsDeleted IS NULL OR IsDeleted = 0);

        IF (@ClonableIRFId IS NULL OR @ClonableIRFId = 0)
        BEGIN
            INSERT INTO [dbo].[ClonableSkillChannel]
                ([Name]
                ,[CreatedOn]
                ,[CreatedBy]
                ,[MeetingPlatform]
                ,[IsAadhaarVerificationRequired]
                ,[AssessmentGroupId]
                ,[IsAIAudioTemplate]
                ,[IsDeleted])
            VALUES
                (@TemplateName
                ,@CreatedOn
                ,@CreatedBy
                ,@MeetingPlatform
                ,@IsAadhaarVerificationRequired
                ,@AssessmentGroupId
                ,1
                ,0);

            SET @ClonableIRFId = SCOPE_IDENTITY();
        END

        IF (@ClonableIRFId IS NOT NULL AND @ClonableIRFId <> 0)
        BEGIN
            DELETE FROM TransClonableIRFTechSkill WHERE ClonableIRFId = @ClonableIRFId;

            -- SkillSet carries the two scored sections through to the channel:
            -- 1 = Role Fitment, 2 = Motivation to Join Fitment.
            INSERT INTO [dbo].[TransClonableIRFTechSkill]
                ([ClonableIRFId]
                ,[LevelId]
                ,[SkillId]
                ,[Weightage]
                ,[Concepts]
                ,[Natureofquestion]
                ,[Timelimit]
                ,[isEliminationskill]
                ,[SkillName]
                ,[SkillSet])
            SELECT @ClonableIRFId
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

        SET @ClonableIRFId = 0;
        THROW;
    END CATCH
END
GO
