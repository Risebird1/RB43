IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_OpenInstaInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_OpenInstaInterviews
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_OpenInstaInterviews]       
   @emailID varchar(200)  
AS      
BEGIN      
;With CTE AS(Select  * from InterviewerProfile cross Apply string_split(AssessmentGroupId,',') where Isinstaavailable=1)    
Select Distinct im.*,ir.*,im.id as id,RequiredSkill=STUFF((SELECT DISTINCT ', ' + CAST(SkillId AS VARCHAR(MAX)) FROM TransInstaSkills t2 WHERE t2.IRFId = tis.IRFId FOR XML PATH('')),1,1,'')          From CTE join InstaRequests ir on ir.AssessmentGroup=CTE.value
join InstaInterviewMeetings im on ir.id=RequestId  
join Userlist ul on CTE.Userid=ul.id  
left join TransInstaSkills tis on tis.IRFId=im.RequestId  
where im.interviewer is null and ISNULL(im.ConfirmationStatus,0) = 0 and ul.EmailId = @emailID     
END      
  
GO