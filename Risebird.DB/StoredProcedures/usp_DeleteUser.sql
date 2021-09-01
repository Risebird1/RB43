/****** Object:  StoredProcedure [dbo].[usp_DeleteUser]    Script Date: 4/30/2021 9:14:15 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_DeleteUser'))
BEGIN
    DROP PROCEDURE dbo.usp_DeleteUser
END


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
  
CREATE PROCEDURE [dbo].[usp_DeleteUser]   
@id int  
AS  
Begin  
  
declare @profile varchar(50)  
  
set @profile = (select [Name] from [Profile] where id = (select idProfile from UserList where id = @id))  
  
if(@profile='Interviewer')  
begin  
  
 declare @interviewerId int  
 set @interviewerId =(select id from InterviewerProfile where UserId=@id)  
  
 delete from TransInterviewerAccDetails where InterviewerProfileId=@interviewerId  
 delete from TransInterviewerAvailability where InterviewerProfileId=@interviewerId  
 delete from TransInterviewerCertificate where InterviewerProfileId=@interviewerId  
 delete from TransInterviewerListInterviewerSkills where InterviewerProfileId=@interviewerId  
 delete from TransInterviewerProfessional where InterviewerProfileId=@interviewerId  
 delete from TransInterviewerProjects where InterviewerProfileId=@interviewerId  
 delete from TransInterviewerSkill where InterviewerProfileId=@interviewerId  
  
 delete from TransInterviewer where UserId=@id  
 delete from TransEmergencyInterviewer where UserId=@id  
 delete from TransQualityController where UserId=@id  
  
end  
  
 delete from SeniorHr where Userid=@id 
 Delete from UserList where id=@id  
End  
  
  
GO

