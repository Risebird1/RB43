IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllInstaInterviewMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllInstaInterviewMeetings
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO    
            
              
                  
CREATE PROCEDURE [dbo].[usp_GetAllInstaInterviewMeetings]                      
@startDate datetime,                  
@endDate datetime,    
@id int,    
@EmailId varchar(100)=null          
AS           
Begin                            
                
--if(@EmailId is not null and ((select idProfile from UserList where EmailId=@EmailId)=2))                
-- Begin              
--    Select * from InstaInterviewMeetings       
--  where RequestedDate >= @startDate AND RequestedDate < @endDate and RequestedBy=@EmailId           
-- End                
--else                
-- Begin                
--    Select * from InstaInterviewMeetings       
--  where RequestedDate >= @startDate AND RequestedDate < @endDate      
-- End                
    Select DISTINCT im.id,im.RequestedDate,im.CandidateName,ResumeLink,ir.JDLink,ir.RequestedBy,    
ConfirmationStatus,MeetingStatus,InterviewStatus,Interviewer,ZoomLink,RequestId,ag.AssessmentGroup,im.AccountEmail,  
RequiredSkill=STUFF((SELECT DISTINCT ', ' + CAST(SkillId AS VARCHAR(MAX)) FROM TransInstaSkills t2 WHERE t2.IRFId = tis.IRFId FOR XML PATH('')),1,1,'')       
from InstaInterviewMeetings im join InstaRequests ir on im.RequestId=ir.id    
join AssessmentGroup ag on ag.id=ir.AssessmentGroup
left join TransInstaSkills tis on tis.IRFId=im.RequestId
where RequestId=@id                      
                           
End                       
            
    
GO          
              
              
     