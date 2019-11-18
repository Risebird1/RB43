/****** Object:  StoredProcedure [dbo].[usp_OpenEmergencyInterviews]    Script Date: 18-11-2019 6.52.24 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.[usp_OpenEmergencyInterviews'))
BEGIN
    DROP PROCEDURE dbo.[usp_OpenEmergencyInterviews]
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




create PROCEDURE [dbo].[usp_OpenEmergencyInterviews]     
   @emailID varchar(200)     
AS    
BEGIN    
select InterviewMeetings.*, irf.JDLink from InterviewMeetings  
 inner join IRF irf on InterviewMeetings.IRFId=irf.IRFId   
 inner join [dbo].[TransEmergencyInterviewer] tirf on IRF.id =tirf.IRFId  
 inner join [dbo].[Interviewer] inte on tirf.InterviewerId = inte.id  
 inner join [dbo].[UserList] ul on inte.UserId = ul.id
 left join UnassignedMeetings um on InterviewMeetings.id=um.InterviewId 
 where interviewer is null and confirmationStatus = 1 and IsEmergency=1 and ul.EmailId = @emailID and um.EmailId !=  @emailID 
    
END 
GO


