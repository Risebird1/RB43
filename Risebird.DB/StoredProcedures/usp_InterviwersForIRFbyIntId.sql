 IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InterviwersForIRFbyIntId'))
BEGIN
    DROP PROCEDURE dbo.usp_InterviwersForIRFbyIntId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO 
Create PROCEDURE [dbo].[usp_InterviwersForIRFbyIntId]    
@irfId int   
AS    
Begin    
    
    
IF EXISTS (Select 1 from IRF where id =@irfId)    
BEGIN    
select distinct userlist.EmailId,userlist.uniqueid from [dbo].[TransInterviewer] tr    
join [dbo].[IRF] irf on tr.IRFId = irf.id    
join [dbo].[UserList] userlist on tr.UserId = userlist.id    
where irf.id = @irfId and tr.IsActive=1 and tr.IsAvailable=1 and userlist.IsActive=1   
END    
--Else    
-- BEGIN    
--  select distinct userlist.EmailId,userlist.uniqueid from [dbo].[TransInterviewerSkillsLeadInterviewer] tr    
--  join [dbo].[InterviewerSkillsChannel] ISC on tr.InterviewerSkillId = ISC.id    
--  join [dbo].[UserList] userlist on tr.UserId = userlist.id    
--  where ISC.InterviewerSkillId like @IRFID    
    
-- END    
     
End    
GO    
  