
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateLastStatusV2'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateLastStatusV2
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_GetCandidateLastStatusV2]    
@dateValue datetime  
AS    
Begin   

;With CTE AS(      
Select max(id) as id from CandidateStatus where cast([Date] as date) = cast(@dateValue as date) group by CandidateId, IRFId)


select cs.IRFId as IRFintId, cs.Comments, csom.StatusValue, irf.IRFId, cs.[Date] as PerformedOn, im.interviewDate, ca.ResumeLink,  
ul.EmailId as candidateEmail, ul.FirstName as candidateName, IRF.JDTiltle as JDTitle, ic.TotalTechScore, ul.MobileNumber, irf.ClientId 
from CandidateStatus cs 
join CTE on CTE.id = cs.id
join CandidateStatusOptionsMst csom on csom.id=cs.[Status]
join IRF on irf.id = cs.IRFId  
join CandidatesMaster cm on cm.id = cs.CandidateId  
join Candidate ca on (ca.CandidateMasterId = cm.id and ca.IRFId = irf.id)  
join UserList ul on ul.id = cm.UserId 
left join InterviewMeetings im on im.id = cs.InterviewId 
left join InterviewComments ic on ic.InterviewId = im.id
where irf.ClientId in (149, 151) -- only synechron 
  
End  
  

