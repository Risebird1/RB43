  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchAllInterviewerupcommingInterviewes'))
BEGIN
    DROP PROCEDURE [dbo].[usp_FetchAllInterviewerupcommingInterviewes]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
    
CREATE Procedure usp_FetchAllInterviewerupcommingInterviewes  
@timeNow datetime   
AS    
BEGIN    
;With CTE as (              
select im.id, im.IRFId, im.interviewDate,im.candidateEmail,         
im.interviewer,ul.EmailId from InterviewMeetings im            
inner join IRF irf on im.IRFId=irf.IRFId             
inner join [dbo].[TransInterviewer] tirf on IRF.id =tirf.IRFId            
left join [dbo].[InterviewerProfile] inte on tirf.InterviewerId = inte.id        
left join [dbo].[UserList] ul on inte.UserId = ul.id         
where interviewer is null and confirmationStatus = 1 and IsEmergency=0 and im.interviewDate>=@timeNow and tirf.IsActive=1)     
Select CTE.EmailId,COUNT(CTE.id) as Total,ip.uniqueid from CTE left join InterviewerPublicURLs ip on ip.Emailid=CTE.EmailId group by CTE.EmailId,ip.uniqueid    
END       
        
        

      
      
  
  GO  
    

  