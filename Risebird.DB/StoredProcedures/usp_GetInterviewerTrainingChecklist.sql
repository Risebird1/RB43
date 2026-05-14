IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviewerTrainingChecklist'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerTrainingChecklist
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetInterviewerTrainingChecklist]
    @InterviewerEmail      VARCHAR(200),
    @TrainingModuleCode    VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        [Id],
        [InterviewerEmail],
        [TrainingModuleCode],
        [ChecklistStateJson],
        [TotalItems],
        [TotalChecked],
        [CreatedBy],
        [CreatedOn]
    FROM [dbo].[InterviewerTrainingChecklist]
    WHERE [InterviewerEmail] = @InterviewerEmail
      AND [TrainingModuleCode] = @TrainingModuleCode
    ORDER BY [CreatedOn] DESC;
END
GO
