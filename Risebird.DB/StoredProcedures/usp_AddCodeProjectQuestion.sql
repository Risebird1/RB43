IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddCodeProjectQuestion'))
BEGIN
    DROP PROCEDURE dbo.usp_AddCodeProjectQuestion
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_AddCodeProjectQuestion
    @IRFId int,
    @SkillName nvarchar(500),
    @Comment nvarchar(max),
    @ExpectedAnswer nvarchar(max) = NULL,
    @QuestionGist nvarchar(max) = NULL,
    @TimeLimitInMinutes int,
    @GeneratedPayload nvarchar(max) = NULL,
    @CreatedBy varchar(150),
    @Language varchar(50),
    @Framework varchar(50) = NULL,
    @StackImageCode varchar(50),
    @EntryPointFile varchar(500) = NULL,
    @ReadOnlyPathsJson nvarchar(max) = NULL,
    @BuildCommand varchar(500) = NULL,
    @RunCommand varchar(500) = NULL,
    @TestCommand varchar(500) = NULL,
    @HasWebserver bit = NULL,
    @PortsJson varchar(200) = NULL,
    @RequirementsJson nvarchar(max) = NULL,
    @EstimatedMinutes int = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRAN

        INSERT INTO SkillChannelSampleQuestions
            (IRFId, Skill, SkillName, Comment, ExpectedAnswer, GeneratedPayload, IsGeneratedByAi, CreatedBy,
             QuestionGist, IsCodingQuestion, IsCodeProjectQuestion, CodingTimeLimitInMinutes)
        VALUES
            (@IRFId, 0, @SkillName, @Comment, @ExpectedAnswer, @GeneratedPayload, 1, @CreatedBy,
             @QuestionGist, 0, 1, @TimeLimitInMinutes);

        DECLARE @NewId int = SCOPE_IDENTITY();

        INSERT INTO CodeProjectDetails
            (QuestionId, Language, Framework, StackImageCode, EntryPointFile, ReadOnlyPathsJson,
             BuildCommand, RunCommand, TestCommand, HasWebserver, PortsJson, RequirementsJson,
             EstimatedMinutes, ValidationStatus)
        VALUES
            (@NewId, @Language, @Framework, @StackImageCode, @EntryPointFile, @ReadOnlyPathsJson,
             @BuildCommand, @RunCommand, @TestCommand, @HasWebserver, @PortsJson, @RequirementsJson,
             @EstimatedMinutes, 'validating');

        COMMIT TRAN
        SELECT @NewId AS NewId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        DECLARE @Message varchar(MAX) = ERROR_MESSAGE(),
                @Severity int = ERROR_SEVERITY(),
                @State smallint = ERROR_STATE();
        RAISERROR (@Message, @Severity, @State);
    END CATCH
END
GO
