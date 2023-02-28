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
if(@Role='SeniorHR' or @Role='SupportAdmin')  
BEGIN  
select (Select Sum(te.totalcount) from (select Count(im.id) as totalcount from InterviewMeetings im join IRF on im.IRFId=IRF.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId  
left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus=1 and ulsh.Emailid= @Emailid  
union   
select Count(im.id) as totalcount from InterviewMeetingsBackup im join IRF on im.IRFId=IRF.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId  
left join UserList ulsh on ulsh.id=sh.Userid left join InterviewCommentsBackup ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus=1 and ulsh.Emailid= @Emailid) as te) as ProxyCount,    
(Select Sum(te.totalcount) from (select Count(im.id) as totalcount from InterviewMeetings im join IRF on im.IRFId=IRF.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId  
left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus=2 and ulsh.Emailid= @Emailid  
union   
select Count(im.id) as totalcount from InterviewMeetingsBackup im join IRF on im.IRFId=IRF.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId  
left join UserList ulsh on ulsh.id=sh.Userid left join InterviewCommentsBackup ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus=2 and ulsh.Emailid= @Emailid) as te) as SelectedCount,    
(Select Sum(te.totalcount) from (select Count(im.id) as totalcount from InterviewMeetings im join IRF on im.IRFId=IRF.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId  
left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus=3 and ulsh.Emailid= @Emailid  
union   
select Count(im.id) as totalcount from InterviewMeetingsBackup im join IRF on im.IRFId=IRF.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId  
left join UserList ulsh on ulsh.id=sh.Userid left join InterviewCommentsBackup ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus=3 and ulsh.Emailid= @Emailid) as te) as RejectedCount,    
(Select Sum(te.totalcount) from (select Count(im.id) as totalcount from InterviewMeetings im join IRF on im.IRFId=IRF.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId  
left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus=4 and ulsh.Emailid= @Emailid  
union   
select Count(im.id) as totalcount from InterviewMeetingsBackup im join IRF on im.IRFId=IRF.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId  
left join UserList ulsh on ulsh.id=sh.Userid left join InterviewCommentsBackup ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus=4 and ulsh.Emailid= @Emailid) as te) as BorderLineCount,    
(Select Sum(te.totalcount) from (select Count(im.id) as totalcount from InterviewMeetings im join IRF on im.IRFId=IRF.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId  
left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus is null and ic.MeetingStatus=1 and ulsh.Emailid= @Emailid  
union   
select Count(im.id) as totalcount from InterviewMeetingsBackup im join IRF on im.IRFId=IRF.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId  
left join UserList ulsh on ulsh.id=sh.Userid left join InterviewCommentsBackup ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus is null and ic.MeetingStatus=1 and ulsh.Emailid= @Emailid) as te) as NoShowCount,    
(Select Sum(te.totalcount) from (select Count(im.id) as totalcount from InterviewMeetings im join IRF on im.IRFId=IRF.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId  
left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus is null and ulsh.Emailid= @Emailid and (ic.MeetingStatus=2 or ic.MeetingStatus is null)  
union   
select Count(im.id) as totalcount from InterviewMeetingsBackup im join IRF on im.IRFId=IRF.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId  
left join UserList ulsh on ulsh.id=sh.Userid left join InterviewCommentsBackup ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus is null and ulsh.Emailid= @Emailid and (ic.MeetingStatus=2 or ic.MeetingStatus is null)) as te) as
 ScheduledCount,    
(Select Sum(te.totalcount) from (select Count(im.id) as totalcount from InterviewMeetings im join IRF on im.IRFId=IRF.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId  
left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus is null and ic.MeetingStatus=3 and ulsh.Emailid= @Emailid  
union   
select Count(im.id) as totalcount from InterviewMeetingsBackup im join IRF on im.IRFId=IRF.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId  
left join UserList ulsh on ulsh.id=sh.Userid left join InterviewCommentsBackup ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus is null and ic.MeetingStatus=3 and ulsh.Emailid= @Emailid) as te) as DroppedCount,    
(Select Sum(te.totalcount) from (select Count(im.id) as totalcount from InterviewMeetings im join IRF on im.IRFId=IRF.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId  
left join UserList ulsh on ulsh.id=sh.Userid left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ulsh.Emailid= @Emailid  
union   
select Count(im.id) as totalcount from InterviewMeetingsBackup im join IRF on im.IRFId=IRF.IRFId join SeniorHr sh on sh.Clientid=irf.ClientId  
left join UserList ulsh on ulsh.id=sh.Userid left join InterviewCommentsBackup ic on ic.InterviewId= im.id where confirmationStatus=1 and ulsh.Emailid= @Emailid) as te) as TotalMeetingsCount    
END  
ELSE  
BEGIN  
select (Select Sum(te.totalcount) from (select Count(im.id) totalcount from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus=1  
union  
select Count(im.id) totalcount from InterviewMeetingsBackup im  left join InterviewCommentsBackup ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus=1) as te) as ProxyCount,    
(Select Sum(te.totalcount) from (select Count(im.id) totalcount from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus=2  
union  
select Count(im.id) totalcount from InterviewMeetingsBackup im  left join InterviewCommentsBackup ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus=2) as te) as SelectedCount,    
(Select Sum(te.totalcount) from (select Count(im.id) totalcount from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus=3  
union  
select Count(im.id) totalcount from InterviewMeetingsBackup im  left join InterviewCommentsBackup ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus=3) as te) as RejectedCount,    
(Select Sum(te.totalcount) from (select Count(im.id) totalcount from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus=4  
union  
select Count(im.id) totalcount from InterviewMeetingsBackup im  left join InterviewCommentsBackup ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus=4) as te) as BorderLineCount,    
(Select Sum(te.totalcount) from (select Count(im.id) totalcount from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1  and ic.InterviewStatus is null and ic.MeetingStatus=1  
union  
select Count(im.id) totalcount from InterviewMeetingsBackup im  left join InterviewCommentsBackup ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus is null and ic.MeetingStatus=1) as te) as NoShowCount,    
(Select Sum(te.totalcount) from (select Count(im.id) totalcount from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1  and ic.InterviewStatus is null and (ic.MeetingStatus=2 or ic.MeetingStatus is null)  
union  
select Count(im.id) totalcount from InterviewMeetingsBackup im  left join InterviewCommentsBackup ic on ic.InterviewId= im.id where confirmationStatus=1  and ic.InterviewStatus is null and (ic.MeetingStatus=2 or ic.MeetingStatus is null)) as te) as ScheduledCount,    
(Select Sum(te.totalcount) from (select Count(im.id) totalcount from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus is null and ic.MeetingStatus=3  
union  
select Count(im.id) totalcount from InterviewMeetingsBackup im  left join InterviewCommentsBackup ic on ic.InterviewId= im.id where confirmationStatus=1 and ic.InterviewStatus is null and ic.MeetingStatus=3) as te) as DroppedCount,    
(Select Sum(te.totalcount) from (select Count(im.id) totalcount from InterviewMeetings im  left join InterviewComments ic on ic.InterviewId= im.id where confirmationStatus=1  
union  
select Count(im.id) totalcount from InterviewMeetingsBackup im  left join InterviewCommentsBackup ic on ic.InterviewId= im.id where confirmationStatus=1) as te) as TotalMeetingsCount    
END    
    
End 
  

GO


