IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddUpdateClientCommentsForReport'))
BEGIN
    DROP PROCEDURE dbo.usp_AddUpdateClientCommentsForReport
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

    
    
Create PROCEDURE [dbo].[usp_AddUpdateClientCommentsForReport]      
      
@Comment varchar(max),
@id int,
@InterviewId int,
@CreatedBy varchar(50),
@timeNow datetime,
@Rating int,
@ClientActionOnInterviewer varchar(25)
      
AS      
Begin      

If Not Exists (Select 1 from ClientCommentsForReport where id = @id and InterviewId = @InterviewId)    
Begin

Insert into ClientCommentsForReport(Comment, InterviewId, CreatedBy, CreatedAt, Rating) 
values (@Comment, @InterviewId, @CreatedBy, @timeNow, @Rating)  

End

else
Begin

update ClientCommentsForReport set 
Comment = @Comment,
ModifiedAt = @timeNow,
Rating = @Rating
where id = @id 

if(@ClientActionOnInterviewer is not null and @ClientActionOnInterviewer != '')
Begin

declare @interviewerUserId int
declare @irfId int
set @interviewerUserId = (select ul.id from UserList ul join InterviewMeetings im on im.interviewer = ul.EmailId where im.id = @InterviewId)
Select @irfId=IRF.id from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId where im.id=@Interviewid


if EXISTS (Select 1 from TransInterviewer where Userid=@interviewerUserId and IRFId=@IRFId)  
BEGIN
if(@ClientActionOnInterviewer='Rejected')
begin
Update TransInterviewer Set ClientAction=@ClientActionOnInterviewer,IsActive=0 Where Userid=@interviewerUserId and IRFId=@irfId
End
Else If(@ClientActionOnInterviewer='Shortlisted')
BEGIN
Update TransInterviewer Set ClientAction=@ClientActionOnInterviewer Where Userid=@interviewerUserId and IRFId=@irfId
END
END

if EXISTS (Select 1 from TransEmergencyInterviewer where Userid=@interviewerUserId and IRFId=@IRFId)  
BEGIN
if(@ClientActionOnInterviewer='Rejected')
begin
Update TransEmergencyInterviewer Set ClientAction=@ClientActionOnInterviewer,IsActive=0 Where Userid=@interviewerUserId and IRFId=@irfId
End
Else If(@ClientActionOnInterviewer='Shortlisted')
BEGIN
Update TransEmergencyInterviewer Set ClientAction=@ClientActionOnInterviewer Where Userid=@interviewerUserId and IRFId=@irfId
END
END

End

End

End 
GO