IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_SaveQcInterviewAIMergedDetails]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_SaveQcInterviewAIMergedDetails]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_SaveQcInterviewAIMergedDetails]
    @qcEvaluationId INT,
    @mergedJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    IF COL_LENGTH('dbo.QcInterviewAIevaluation', 'MergedAIQCDetails') IS NULL
    BEGIN
        RAISERROR('Column MergedAIQCDetails not found on QcInterviewAIevaluation. Run the alter script first.', 16, 1);
        RETURN;
    END

    UPDATE [dbo].[QcInterviewAIevaluation]
    SET [MergedAIQCDetails] = @mergedJson
    WHERE [id] = @qcEvaluationId;
END
GO
