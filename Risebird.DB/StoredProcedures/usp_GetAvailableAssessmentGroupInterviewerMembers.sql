IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAvailableAssessmentGroupInterviewerMembers'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAvailableAssessmentGroupInterviewerMembers
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_GetAvailableAssessmentGroupInterviewerMembers      
@assessmentGroupId int      
AS      
BEGIN      
;With CTE AS(      
Select UserId, value from InterviewerProfile ipr cross Apply string_split(AssessmentGroupId,',') where ipr.Isavailable = 1 and AvailabilityResponseDate is not null)     
Select distinct ul.EmailId, ul.uniqueid, (ul.FirstName+' '+ul.LastName) as InterviewerName        
From CTE join Userlist ul on CTE.Userid=ul.id       
where CTE.value = @assessmentGroupId and ul.IsActive=1    
END
GO

  
      