IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_CheckAiContradictionDetails]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_CheckAiContradictionDetails]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_CheckAiContradictionDetails]  
@qcEvaluationId INT
AS  
BEGIN  
    SELECT      
        q.[id],      
        q.[InterviewId],      
        IRF.[id] as irfId,    
        q.[ReportAIDetails],      
        q.[TranscritAiDetails],    
        ic.[TranscriptSummary],    
        q.[ContradictionJson],    
        imj.[Joinedon],    
        im.interviewDate,    
        ic.VideoDuration as ActualDuration,    
        pt.DurationSelectedMin as RequiredDuration,  
        ic.AudioTranscriptFileLink,
        ic.MeetingStatus,
        cfd.Rating as CandidateRating,
        CAST(CASE WHEN EXISTS(SELECT 1 FROM RedoInterviews ri WHERE ri.interviewid = im.id) THEN 1 ELSE 0 END AS BIT) as CandidateRedoRequest,
        cfd.InterviewerVideo as InterviewerVideoOff
    FROM InterviewMeetings im 
    JOIN InterviewComments ic ON ic.InterviewId = im.Id    
    LEFT JOIN [dbo].[QcInterviewAIevaluation] q ON im.id = q.InterviewId         
    JOIN IRF ON IRF.IRFId = im.IRFId    
    LEFT JOIN PlanTypeChargesMaster pt ON pt.CountryType = IRF.JobCountry AND pt.PlanTypeEnumValue = IRF.Heuristicsscore AND IRF.CompanyType = pt.CompanyType    
    OUTER APPLY(SELECT MIN(Joinedon) as JoinedOn FROM InterviewMeetingsJoinTime WHERE Interviewid = im.Id AND interviewer = im.Interviewer) imj    
    LEFT JOIN CandidateFeedbackDetails cfd ON cfd.InterviewId = im.id
    WHERE im.id = @qcEvaluationId  
END 
GO
