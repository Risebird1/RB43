IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetPendingQcInterviewAIMerge]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_GetPendingQcInterviewAIMerge]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
  
-- Pending rows: human QC not done, both Report AI and Transcript AI JSON present, and merged output not yet stored.  
CREATE PROCEDURE [dbo].[usp_GetPendingQcInterviewAIMerge]  
AS  
BEGIN  
    SET NOCOUNT ON;  
  
    IF COL_LENGTH('dbo.QcInterviewAIevaluation', 'MergedAIQCDetails') IS NULL  
    BEGIN  
        RAISERROR('Column MergedAIQCDetails not found on QcInterviewAIevaluation. Run the alter script first.', 16, 1);  
        RETURN;  
    END  
  
    SELECT  
        q.[id],  
        q.[InterviewId],  
		IRF.[id] as irfId,
        q.[ReportAIDetails],  
        q.[TranscritAiDetails],
		ic.[TranscriptSummary],
		q.[ContradictionJson]
    FROM [dbo].[QcInterviewAIevaluation] q  
	join InterviewMeetings im on im.id=q.InterviewId
	join InterviewComments ic on ic.InterviewId=q.InterviewId
	join IRF on IRF.IRFId=im.IRFId
    WHERE (q.[QCDone] IS NULL OR q.[QCDone] = 0)  
      AND q.[ReportAIDetails] IS NOT NULL AND LEN(LTRIM(RTRIM(q.[ReportAIDetails]))) > 0  
      AND q.[TranscritAiDetails] IS NOT NULL AND LEN(LTRIM(RTRIM(q.[TranscritAiDetails]))) > 0  
END  
GO
