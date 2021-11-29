/****** Object:  StoredProcedure [dbo].[usp_GetAccounts]    Script Date: 24-03-2020 8.20.45 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateUploadedVideoLink'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateUploadedVideoLink
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
  
  
  
CREATE PROCEDURE [dbo].[usp_UpdateUploadedVideoLink]   
@interviewId int,  
@videoLink varchar(1000),  
@videoDuration int  
AS    
Begin   
  
update InterviewComments set VideoLink=@videoLink, VideoDuration = @videoDuration where InterviewId=@interviewId  
  
declare  
 @interviewerUserId int,  
 @irfId int,  
 @perSuccessRate int,  
 @meetingStatus int,  
 @interviewStatus int,  
 @updatedPoints int,  
 @candidateEmail varchar(200)  
  
  
 set @interviewerUserId  = (select id from UserList where EmailId=(select interviewer from InterviewMeetings where id=@interviewId))  
 set @irfId = (select id from IRF where IRFId=(select IRFId from InterviewMeetings where id=@interviewId))  
 set @meetingStatus=(select MeetingStatus from InterviewComments where InterviewId=@interviewId)  
 set @interviewStatus=(select InterviewStatus from InterviewComments where InterviewId=@interviewId)  
 set @candidateEmail= (select candidateEmail from InterviewMeetings where id = @interviewId)  
  
  
if(@meetingStatus is not null and @meetingStatus!=2)  
Begin  
 declare @IsEmergency bit  
 select @IsEmergency = IsEmergency from InterviewMeetings where id=@interviewId  
  
 If(@IsEmergency=1)  
 Begin  
 IF EXISTS(select 1 from TransEmergencyInterviewer where IRFId=@irfId and UserId=@interviewerUserId)  
  BEGIN  
  set @perSuccessRate = (select PerSuccessRate from TransEmergencyInterviewer where IRFId=@irfId and UserId=@interviewerUserId)  
  set @updatedPoints= dbo.func_ComputeInterviewerPoints(@perSuccessRate, @videoDuration, @meetingStatus, @interviewStatus)  
    
  IF NOT EXISTS(select 1 from InterviewPoints where Interviewid = @interviewId and userid=@interviewerUserId and [Type]='Interview')  
   BEGIN  
    
  
    insert into InterviewPoints(Interviewid, userid, Points, [Type], Comment, [Status], [Date])  
    values (@interviewId, @interviewerUserId, @updatedPoints, 'Interview', @candidateEmail, 'Pending', getdate())  
  
   END  
  
  ELSE  
   BEGIN  
    update InterviewPoints set Points = @updatedPoints, [Date]=getdate()  
    where Interviewid = @interviewId and userid=@interviewerUserId and [Type]='Interview'  
   END  
  END  
  else IF EXISTS(select 1 from TransInterviewer where IRFId=@irfId and UserId=@interviewerUserId)  
  BEGIN
   set @perSuccessRate = (select PerSuccessRate from TransInterviewer where IRFId=@irfId and UserId=@interviewerUserId)  
   set @updatedPoints= dbo.func_ComputeInterviewerPoints(@perSuccessRate, @videoDuration, @meetingStatus, @interviewStatus)  
   IF NOT EXISTS(select 1 from InterviewPoints where Interviewid = @interviewId and userid=@interviewerUserId and [Type]='Interview')  
    BEGIN  
     insert into InterviewPoints(Interviewid, userid, Points, [Type], Comment, [Status], [Date])  
     values (@interviewId, @interviewerUserId, @updatedPoints, 'Interview', @candidateEmail, 'Pending', getdate())  
    END  
   ELSE  
    BEGIN  
     update InterviewPoints set Points = @updatedPoints, [Date]=getdate()  
     where Interviewid = @interviewId and userid=@interviewerUserId and [Type]='Interview'  
    END  
  END

 End  
 Else  
 Begin  
 IF EXISTS(select 1 from TransInterviewer where IRFId=@irfId and UserId=@interviewerUserId)  
  BEGIN  
   set @perSuccessRate = (select PerSuccessRate from TransInterviewer where IRFId=@irfId and UserId=@interviewerUserId)  
  
   set @updatedPoints= dbo.func_ComputeInterviewerPoints(@perSuccessRate, @videoDuration, @meetingStatus, @interviewStatus)  
   IF NOT EXISTS(select 1 from InterviewPoints where Interviewid = @interviewId and userid=@interviewerUserId and [Type]='Interview')  
    BEGIN  
     insert into InterviewPoints(Interviewid, userid, Points, [Type], Comment, [Status], [Date])  
     values (@interviewId, @interviewerUserId, @updatedPoints, 'Interview', @candidateEmail, 'Pending', getdate())  
    END  
   ELSE  
    BEGIN  
     update InterviewPoints set Points = @updatedPoints, [Date]=getdate()  
     where Interviewid = @interviewId and userid=@interviewerUserId and [Type]='Interview'  
    END  
  END  
 ELSE IF EXISTS(select 1 from TransEmergencyInterviewer where IRFId=@irfId and UserId=@interviewerUserId)  
  BEGIN  
  set @perSuccessRate = (select PerSuccessRate from TransEmergencyInterviewer where IRFId=@irfId and UserId=@interviewerUserId)  
  set @updatedPoints= dbo.func_ComputeInterviewerPoints(@perSuccessRate, @videoDuration, @meetingStatus, @interviewStatus)  
  IF NOT EXISTS(select 1 from InterviewPoints where Interviewid = @interviewId and userid=@interviewerUserId and [Type]='Interview')  
   BEGIN  
    insert into InterviewPoints(Interviewid, userid, Points, [Type], Comment, [Status], [Date])  
    values (@interviewId, @interviewerUserId, @updatedPoints, 'Interview', @candidateEmail, 'Pending', getdate())  
   END  
  ELSE  
   BEGIN  
    update InterviewPoints set Points = @updatedPoints, [Date]=getdate()  
    where Interviewid = @interviewId and userid=@interviewerUserId and [Type]='Interview'  
   END  
  END  
  
 Else  
  
  Begin  
   IF NOT EXISTS(select 1 from InterviewPoints where Interviewid = @interviewId and userid=@interviewerUserId and [Type]='Interview')  
    BEGIN  
    
  
      insert into InterviewPoints(Interviewid, userid, Points, [Type], Comment, [Status], [Date])  
      values (@interviewId, @interviewerUserId, 0, 'Interview', @candidateEmail, 'Pending', getdate())  
  
    END  
  
   ELSE  
    BEGIN  
     update InterviewPoints set Points = 0, [Date]=getdate()  
     where Interviewid = @interviewId and userid=@interviewerUserId and [Type]='Interview'  
    END  
  END  
 End  
  
  
  
  
   
    
End  
  
  
  
  
End  
GO


