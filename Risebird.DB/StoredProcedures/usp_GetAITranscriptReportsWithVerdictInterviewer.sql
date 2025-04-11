  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetAITranscriptReportsWithVerdictInterviewer'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAITranscriptReportsWithVerdictInterviewer
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetAITranscriptReportsWithVerdictInterviewer]      
@interviewer varchar(150),    
@Irfid varchar(15)  
As    
Begin    
 Select distinct im.id as InterviewId, IRF.id as irfIntId, im.interviewDate,IRF.IRFId,    
 im.candidateName, im.candidateEmail, im.interviewer, ul.MobileNumber as InterviewerMobileNumber,    
 uli.EmailId as ChannelOwner, ic.InterviewStatus,    
 im.confirmationStatus, ic.MeetingStatus, ic.AudioTranscriptFileLink,     
 ic.IsAITranscriptAnalysisAttempted, ic.IsAITranscriptAnalysisFailed,    
 ic.AITranscriptVerdict, ic.AIVerdict as AIQCVerdict,ic.AITranscriptAnalysisScore    
 from InterviewMeetings im     
 join IRF on IRF.IRFId=im.IRFId    
 join [dbo].[InterviewComments] ic on im.id = ic.InterviewId    
 left join UserList ul on ul.EmailId=im.interviewer    
 join UserList uli on uli.id= irf.RisebirdHr    
 where ic.AITranscriptVerdict in ('yes') and -- ic.AITranscriptVerdict is not  null  
 (Isnull(@Irfid,'')='' or IRF.IRFId=@Irfid) and im.interviewer=@interviewer  
    
End 
GO