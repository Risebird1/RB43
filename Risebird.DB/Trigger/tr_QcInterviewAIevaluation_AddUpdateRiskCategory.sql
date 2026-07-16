IF EXISTS (SELECT 1 FROM sys.triggers
           WHERE Name = 'tr_QcInterviewAIevaluation_AddUpdateRiskCategory')
BEGIN
    DROP TRIGGER tr_QcInterviewAIevaluation_AddUpdateRiskCategory
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Refreshes RiskCategory when QC AI redo / contradiction / merge fields change.
CREATE TRIGGER tr_QcInterviewAIevaluation_AddUpdateRiskCategory
ON dbo.QcInterviewAIevaluation
AFTER INSERT, UPDATE
AS
BEGIN
    DECLARE @InterviewId INT;
    SELECT TOP (1) @InterviewId = InterviewId
    FROM inserted
    WHERE InterviewId IS NOT NULL AND InterviewId > 0;

    IF @InterviewId IS NOT NULL
        EXEC dbo.usp_AddUpdateRiskCategory @InterviewId = @InterviewId;
END
GO
