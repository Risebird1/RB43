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
Select Distinct 'Interviewer' as [Type],CTE.UserId,CTE.AssessmentGroupId, ul.EmailId, ul.MobileNumber,ul.uniqueid,(ul.FirstName+ space(1) +ul.LastName) as InterviewerName,count(im.id) as TotalInterviews, CTE.ResumeLink,CTE.Isavailable as AvailableStatus
From CTE join Userlist ul on CTE.Userid=ul.id 
left join InterviewMeetings im on im.interviewer=ul.EmailId
where CTE.value = @Assessmentid and (im.confirmationStatus=1 or im.confirmationStatus is null)
group by CTE.UserId,CTE.AssessmentGroupId, ul.EmailId, ul.MobileNumber,ul.uniqueid,ul.FirstName+space(1)+ul.LastName,CTE.ResumeLink,CTE.Isavailable

END
GO

  
      