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
Select Distinct im.* From CTE join InstaInterviewMeetings im on im.AssessmentGroup=CTE.value
join Userlist ul on CTE.Userid=ul.id
where im.interviewer is null and ISNULL(im.ConfirmationStatus,0) = 0 and ul.EmailId = @emailID 
END    
  
GO