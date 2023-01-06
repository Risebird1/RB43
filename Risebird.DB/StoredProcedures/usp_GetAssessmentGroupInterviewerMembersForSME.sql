IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAssessmentGroupInterviewerMembersForSME'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAssessmentGroupInterviewerMembersForSME
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_GetAssessmentGroupInterviewerMembersForSME      
@Assessmentid int      
AS      
BEGIN 

If Not exists(select top 1 id from AssessmentGroupInterviewers where AssessmentGroupId = @Assessmentid)
Begin
	;With CTE AS(      
	Select * from InterviewerProfile cross Apply string_split(AssessmentGroupId,','))     
	Select Distinct CTE.UserId, CTE.id as InterviewerProfileId, ul.EmailId, CTE.value as AssessmentGroupId, (ul.FirstName+ space(1) +ul.LastName) as InterviewerName, CTE.Isavailable as AvailableStatus, ul.IsActive, iav.IsVerified     
	From CTE join Userlist ul on CTE.Userid=ul.id       
	left join InterviewerAssessmentVerifications iav on iav.UserId=ul.id and iav.AssessmentId=CTE.value  
	where CTE.value = @Assessmentid and ul.IsActive = 1-- and iav.IsVerified = 1 
End
Else
Begin
	select agi.AssessmentGroupId, agi.InterviewerProfileId, agi.IsSME, ul.EmailId, (ul.FirstName+ space(1) +ul.LastName) as InterviewerName  from AssessmentGroupInterviewers agi
	join InterviewerProfile ipr on ipr.id = agi.InterviewerProfileId
	join UserList ul on ul.id = ipr.UserId
	where agi.AssessmentGroupId = @Assessmentid
End
 
 
         
      
END  
GO



 
  
      