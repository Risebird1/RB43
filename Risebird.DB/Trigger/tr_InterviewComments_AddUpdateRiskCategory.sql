IF EXISTS (SELECT 1 FROM sys.triggers
           WHERE Name = 'tr_InterviewComments_AddUpdateRiskCategory')
BEGIN
    DROP TRIGGER tr_InterviewComments_AddUpdateRiskCategory
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Refreshes RiskCategory when risk-relevant InterviewComments columns change.
CREATE TRIGGER tr_InterviewComments_AddUpdateRiskCategory
ON dbo.InterviewComments
AFTER  UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF TRIGGER_NESTLEVEL() > 1
        RETURN;

    IF EXISTS (SELECT 1 FROM deleted)
       AND NOT (
            UPDATE(MeetingStatus)
         OR UPDATE(VideoDuration)
         OR UPDATE(InterviewStatus)
         OR UPDATE(InterviewerComment)
         OR UPDATE(AITranscriptAnalysisScore)
         OR UPDATE(AIReportQCAnalysisScore)
       )
        RETURN;

    DECLARE @InterviewId INT;
    SELECT TOP (1) @InterviewId = InterviewId
    FROM inserted
    WHERE InterviewId IS NOT NULL AND InterviewId > 0;

    IF @InterviewId IS NOT NULL
        EXEC dbo.usp_AddUpdateRiskCategory @InterviewId = @InterviewId;
END
GO
