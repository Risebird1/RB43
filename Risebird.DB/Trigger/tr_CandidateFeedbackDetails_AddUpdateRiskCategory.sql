IF EXISTS (SELECT 1 FROM sys.triggers
           WHERE Name = 'tr_CandidateFeedbackDetails_AddUpdateRiskCategory')
BEGIN
    DROP TRIGGER tr_CandidateFeedbackDetails_AddUpdateRiskCategory
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Refreshes RiskCategory when candidate feedback (Rating) is inserted/updated.
CREATE TRIGGER tr_CandidateFeedbackDetails_AddUpdateRiskCategory
ON dbo.CandidateFeedbackDetails
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF TRIGGER_NESTLEVEL() > 1
        RETURN;

    IF EXISTS (SELECT 1 FROM deleted)
       AND NOT UPDATE(Rating)
       AND NOT UPDATE(InterviewId)
        RETURN;

    DECLARE @InterviewId INT;
    SELECT TOP (1) @InterviewId = InterviewId
    FROM inserted
    WHERE InterviewId IS NOT NULL AND InterviewId > 0;

    IF @InterviewId IS NOT NULL
        EXEC dbo.usp_AddUpdateRiskCategory @InterviewId = @InterviewId;
END
GO
