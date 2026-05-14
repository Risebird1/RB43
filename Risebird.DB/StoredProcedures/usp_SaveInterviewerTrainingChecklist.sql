IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveInterviewerTrainingChecklist'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveInterviewerTrainingChecklist
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_SaveInterviewerTrainingChecklist]
    @InterviewerEmail      VARCHAR(200),
    @TrainingModuleCode    VARCHAR(50),
    @ChecklistStateJson    NVARCHAR(MAX),
    @TotalItems            INT,
    @TotalChecked          INT,
    @CreatedBy             VARCHAR(200),
    @NewId                 INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM [dbo].[InterviewerTrainingChecklist]
        WHERE [InterviewerEmail] = @InterviewerEmail
          AND [TrainingModuleCode] = @TrainingModuleCode
    )
    BEGIN
        UPDATE [dbo].[InterviewerTrainingChecklist]
        SET [ChecklistStateJson] = @ChecklistStateJson,
            [TotalItems]         = @TotalItems,
            [TotalChecked]       = @TotalChecked,
            [CreatedBy]          = @CreatedBy,
            [CreatedOn]          = GETUTCDATE()
        WHERE [InterviewerEmail] = @InterviewerEmail
          AND [TrainingModuleCode] = @TrainingModuleCode;

        SELECT @NewId = [Id]
        FROM [dbo].[InterviewerTrainingChecklist]
        WHERE [InterviewerEmail] = @InterviewerEmail
          AND [TrainingModuleCode] = @TrainingModuleCode;
    END
    ELSE
    BEGIN
        INSERT INTO [dbo].[InterviewerTrainingChecklist]
        (
            [InterviewerEmail],
            [TrainingModuleCode],
            [ChecklistStateJson],
            [TotalItems],
            [TotalChecked],
            [CreatedBy],
            [CreatedOn]
        )
        VALUES
        (
            @InterviewerEmail,
            @TrainingModuleCode,
            @ChecklistStateJson,
            @TotalItems,
            @TotalChecked,
            @CreatedBy,
            GETUTCDATE()
        );

        SET @NewId = SCOPE_IDENTITY();
    END
END
GO
