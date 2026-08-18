IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetPendingQcInterviewAIMergeById]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_GetPendingQcInterviewAIMergeById]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_GetPendingQcInterviewAIMergeById]     
@interviewId int   
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
  pt.DurationSelectedMin as RequiredDuration ,  
  ic.AudioTranscriptFileLink,
  ic.MeetingStatus,
  cfd.Rating as CandidateRating,
  CAST(CASE WHEN EXISTS(SELECT 1 FROM RedoInterviews ri WHERE ri.interviewid = im.id) THEN 1 ELSE 0 END AS BIT) as CandidateRedoRequest,
  cfd.InterviewerVideo as InterviewerVideoOff
    FROM InterviewMeetings im 
 join InterviewComments ic on ic.InterviewId=im.Id    
 left join [dbo].[QcInterviewAIevaluation] q on im.id=q.InterviewId         
 join IRF on IRF.IRFId=im.IRFId    
 Left JOIN PlanTypeChargesMaster pt ON pt.CountryType = IRF.JobCountry AND pt.PlanTypeEnumValue = IRF.Heuristicsscore AND IRF.CompanyType = pt.CompanyType    
 outer Apply(Select Min(Joinedon) as JoinedOn From InterviewMeetingsJoinTime where Interviewid=q.InterviewId and interviewer=im.Interviewer) imj    
 LEFT JOIN CandidateFeedbackDetails cfd ON cfd.InterviewId = im.id
    WHERE im.id=@interviewId  
END   
GO