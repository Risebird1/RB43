/****** Object:  StoredProcedure [dbo].[usp_GetMeetingsResults]    Script Date: 30-07-2020 8.07.03 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetMeetingsResults'))
BEGIN
    DROP PROCEDURE dbo.usp_GetMeetingsResults
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
CREATE PROCEDURE [dbo].[usp_GetMeetingsResults]  
@Emailid varchar(150)
AS    
Begin   
 Declare @Role varchar(150)
Select @Role=pr.Name From UserList ul join Profile pr on pr.id=ul.idprofile where Emailid=@Emailid
if(@Role='SeniorHR')
BEGIN
select (select Count(im.id) from InterviewMeetings im join IRF on im.IRFId=IRF.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId
left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus=1 and ulsh.Emailid= @Emailid) as ProxyCount,  
(select Count(im.id) from InterviewMeetings im join IRF on im.IRFId=IRF.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId
left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus=2 and ulsh.Emailid= @Emailid) as SelectedCount,  
(select Count(im.id) from InterviewMeetings im join IRF on im.IRFId=IRF.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId
left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus=3 and ulsh.Emailid= @Emailid) as RejectedCount,  
(select Count(im.id) from InterviewMeetings im join IRF on im.IRFId=IRF.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId
left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus=4 and ulsh.Emailid= @Emailid) as BorderLineCount,  
(select Count(im.id) from InterviewMeetings im join IRF on im.IRFId=IRF.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId
left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus is null and ic.MeetingStatus=1 and ulsh.Emailid= @Emailid) as NoShowCount,  
(select Count(im.id) from InterviewMeetings im join IRF on im.IRFId=IRF.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId
left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus is null and ulsh.Emailid= @Emailid and (ic.MeetingStatus=2 or ic.MeetingStatus is null)) as ScheduledCount,  
(select Count(im.id) from InterviewMeetings im join IRF on im.IRFId=IRF.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId
left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus is null and ic.MeetingStatus=3 and ulsh.Emailid= @Emailid) as DroppedCount,  
(select Count(im.id) from InterviewMeetings im join IRF on im.IRFId=IRF.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId
left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ulsh.Emailid= @Emailid) as TotalMeetingsCount  
END
ELSE
BEGIN
select (select Count(im.id) from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus=1) as ProxyCount,  
(select Count(im.id) from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus=2) as SelectedCount,  
(select Count(im.id) from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus=3) as RejectedCount,  
(select Count(im.id) from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus=4) as BorderLineCount,  
(select Count(im.id) from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus is null and ic.MeetingStatus=1) as NoShowCount,  
(select Count(im.id) from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus is null and (ic.MeetingStatus=2 or ic.MeetingStatus is null)) as ScheduledCount,  
(select Count(im.id) from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus is null and ic.MeetingStatus=3) as DroppedCount,  
(select Count(im.id) from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1) as TotalMeetingsCount  
END  
  
End  
  

GO


