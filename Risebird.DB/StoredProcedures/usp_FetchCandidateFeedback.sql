IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchCandidateFeedback'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchCandidateFeedback
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure usp_FetchCandidateFeedback  
@Emailid varchar(200),  
@Profile varchar(50)  
AS  
BEGIN  
  
if(@Profile='Client')  
BEGIN  
 Declare @UserId int  
 Declare @Clientid int  
  
 select @Clientid=cl.id from [ClientUser] cu inner join [dbo].[UserList] ul on cu.UserId = ul.id inner join [dbo].[Client] cl on cu.ClientId= cl.id      
 where ul.IsActive =1 and ul.emailid =@Emailid    
 Select @UserId=id from UserList where Emailid=@Emailid  
   
 Select cfd.*,IRF.JDTiltle as PrimarySkill,im.Interviewer as interviewerEmail,im.CandidateEmail as EmailAddress,1 as IsClient,im.Interviewdate from CandidateFeedbackDetails cfd join InterviewMeetings im on cfd.Interviewid=im.id join IRF on im.IRFId=IRF.IRFId  
 join TransClientUser tcu on tcu.IRFId=IRF.id   
 where IRF.ClientId=@Clientid and tcu.ClientUserId=@UserId and im.confirmationStatus=1  
END  
else if(@Profile='SeniorHR' or @Profile='SupportAdmin')  
BEGIN  
 Select cfd.*,IRF.JDTiltle as PrimarySkill,im.Interviewer as interviewerEmail,im.CandidateEmail as EmailAddress,1 as IsClient,im.Interviewdate from CandidateFeedbackDetails cfd  join InterviewMeetings im on cfd.Interviewid=im.id join IRF on im.IRFId=IRF.IRFId  
 join SeniorHR sh on IRF.ClientId=sh.Clientid join Userlist ul on ul.id=Sh.Userid   
 where ul.Emailid=@Emailid and im.confirmationStatus=1  
END  
else  
BEGIN  
 Select cfd.*,IRF.JDTiltle as PrimarySkill,im.Interviewer as interviewerEmail,im.CandidateEmail as EmailAddress,im.Interviewdate from CandidateFeedbackDetails cfd  join InterviewMeetings im on cfd.Interviewid=im.id join IRF on im.IRFId=IRF.IRFId  
 where im.confirmationStatus=1  
END  
  
END  
Go