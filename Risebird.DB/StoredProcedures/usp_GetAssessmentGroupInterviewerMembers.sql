IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAssessmentGroupInterviewerMembers'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAssessmentGroupInterviewerMembers
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_GetAssessmentGroupInterviewerMembers      
@Assessmentid int      
AS      
BEGIN      
;With CTE AS(      
Select * from InterviewerProfile cross Apply string_split(AssessmentGroupId,','))     
Select Distinct 'Interviewer' as [Type],CTE.UserId,CTE.AssessmentGroupId, ul.EmailId, ul.MobileNumber,ul.uniqueid,
(ul.FirstName+ space(1) +ul.LastName) as InterviewerName,
count(im.id) as TotalInterviews, CTE.ResumeLink,CTE.Isavailable as AvailableStatus,ul.IsActive,
iav.MCQCandidateid,ag.AssessmentVerificationType,ag.CodeLanguage,ms.PartitionKey as MCQSetsValue,cid.inviteId,
msl.PartitionKey as SMCQId,CTE.isVerified,CTE.IsEmployed,CTE.CompanyName,CTE.EmploymentComment,CTE.AvailabilityResponseDate         
From CTE join Userlist ul on CTE.Userid=ul.id       
left join InterviewMeetings im on im.interviewer=ul.EmailId  and (im.confirmationStatus=1 or im.confirmationStatus is null)      
left join InterviewerAssessmentVerifications iav on iav.UserId=ul.id and iav.AssessmentId=CTE.value  
left join AssessmentGroup ag on ag.id=CTE.value  
left join MCQSets ms on ms.id=ag.MCQSetsValue  
left join CodeInviteDetails cid on cid.ProgrammingLanguage= ag.CodeLanguage and cid.candidateEmail=ul.EmailId
left join MCQShareList msl on msl.MCQId=ms.PartitionKey and msl.EmailAddress=ul.EmailId
where CTE.value = @Assessmentid    
group by CTE.UserId,CTE.AssessmentGroupId, ul.EmailId, ul.MobileNumber,ul.uniqueid,ul.FirstName+space(1)+ul.LastName,CTE.ResumeLink,CTE.Isavailable,ul.IsActive,iav.MCQCandidateid,ag.AssessmentVerificationType,ag.CodeLanguage,ms.PartitionKey,cid.inviteId,msl.PartitionKey,CTE.isVerified,CTE.IsEmployed,CTE.CompanyName,CTE.EmploymentComment,CTE.AvailabilityResponseDate 
END
GO

  
      