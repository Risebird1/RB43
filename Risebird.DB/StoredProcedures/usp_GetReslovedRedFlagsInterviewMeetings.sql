IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetReslovedRedFlagsInterviewMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetReslovedRedFlagsInterviewMeetings
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_GetReslovedRedFlagsInterviewMeetings]                            
AS                 
Begin                                  
select distinct im.id imid,im.IRFId, im.interviewDate,im.candidateEmail,im.ClientHrContact, im.interviewer,                  
im.candidateName,IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,IRF.ClientName,ic.VideoLink,ic.InterviewerComment,            
ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus,ul.EmailId as risebirdHR,    
im.HRCoordinatorEmailId,cl.[Name] as Client,tqc.id as QCPresent    
from Trans_QCFormReport tqc join InterviewMeetings im on tqc.Interviewid=im.id                   
inner join IRF on im.IRFId = IRF.IRFId     
join USerList uli on uli.EmailId=im.candidateEmail  
left join UserList ul on ul.id=irf.RisebirdHr    
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId                  
left join Client cl on cl.id = irf.ClientId    
where tqc.InterviewerAction=1
End  
  GO
  