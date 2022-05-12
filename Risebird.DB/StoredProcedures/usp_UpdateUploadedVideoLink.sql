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
 @candidateEmail varchar(200),    
 @jobCountry varchar(20)  
 Declare @IsProctor bit   
        
        
 set @interviewerUserId  = (select id from UserList where EmailId=(select interviewer from InterviewMeetings where id=@interviewId))        
 set @irfId = (select id from IRF where IRFId=(select IRFId from InterviewMeetings where id=@interviewId))        
 set @meetingStatus=(select MeetingStatus from InterviewComments where InterviewId=@interviewId)        
 set @interviewStatus=(select InterviewStatus from InterviewComments where InterviewId=@interviewId)        
 set @candidateEmail= (select candidateEmail from InterviewMeetings where id = @interviewId)        
 set @jobCountry = (select JobCountry from IRF where id=@irfId)           
 Select @IsProctor=ISNULL(ca.IsProctor,0) from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId join Userlist ul on im.candidateEmail=ul.EmailId   
 join CandidatesMaster cm on ul.id=cm.UserId join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id  where im.id=@interviewId                   
   
if(@meetingStatus is not null and @meetingStatus!=2)        
Begin        
 declare @IsEmergency bit        
 select @IsEmergency = IsEmergency from InterviewMeetings where id=@interviewId        
        
 If(@IsEmergency=1)        
 Begin        
 IF EXISTS(select 1 from TransEmergencyInterviewer where IRFId=@irfId and UserId=@interviewerUserId)        
  BEGIN        
  set @perSuccessRate = (select PerSuccessRate from TransEmergencyInterviewer where IRFId=@irfId and UserId=@interviewerUserId)        
  set @updatedPoints= dbo.func_ComputeInterviewerPoints(@perSuccessRate, @videoDuration, @meetingStatus, @interviewStatus,@jobCountry)        
  if(@updatedPoints<0)  
   BEGIN  
   set @candidateEmail='Penalty '+@candidateEmail  
   END   
   if(@IsProctor=1)  
  BEGIN  
  if(@meetingStatus=4)
	BEGIN
		-- set for Selected or Rejected status with time check
		if(@videoDuration<=90)
		BEGIN
		set @updatedPoints=150  
		END
		else
		BEGIN
		set @updatedPoints=250  
		END
	END
	else if(@meetingStatus=1)
	BEGIN
	set @updatedPoints=50
	END
	else if(@meetingStatus=3)
	BEGIN
		if(@videoDuration<=30)
		BEGIN
		set @updatedPoints=75  
		END
		else if(@videoDuration<=90 and @videoDuration>30)
		BEGIN
		set @updatedPoints=150  
		END
		else 
		BEGIN
		set @updatedPoints=250  
		END
	END
  set @candidateEmail=REPLACE(@candidateEmail,'Penalty','')  
  END  
  IF NOT EXISTS(select 1 from InterviewPoints where Interviewid = @interviewId and userid=@interviewerUserId and [Type]='Interview')        
   BEGIN        
          
        
    insert into InterviewPoints(Interviewid, userid, Points, [Type], Comment, [Status], [Date])        
    values (@interviewId, @interviewerUserId, @updatedPoints, 'Interview', @candidateEmail, 'Pending', getdate())        
        
   END        
        
  ELSE        
   BEGIN        
    update InterviewPoints set Points = @updatedPoints, [Date]=getdate(),Comment=@candidateEmail        
    where Interviewid = @interviewId and userid=@interviewerUserId and [Type]='Interview'        
   END        
  END        
  else IF EXISTS(select 1 from TransInterviewer where IRFId=@irfId and UserId=@interviewerUserId)        
  BEGIN      
   set @perSuccessRate = (select PerSuccessRate from TransInterviewer where IRFId=@irfId and UserId=@interviewerUserId)        
   set @updatedPoints= dbo.func_ComputeInterviewerPoints(@perSuccessRate, @videoDuration, @meetingStatus, @interviewStatus,@jobCountry)        
   if(@updatedPoints<0)  
   BEGIN  
   set @candidateEmail='Penalty '+@candidateEmail  
   END  
    if(@IsProctor=1)  
  BEGIN  
  if(@meetingStatus=4)
	BEGIN
		-- set for Selected or Rejected status with time check
		if(@videoDuration<=90)
		BEGIN
		set @updatedPoints=150  
		END
		else
		BEGIN
		set @updatedPoints=250  
		END
	END
	else if(@meetingStatus=1)
	BEGIN
	set @updatedPoints=50
	END
	else if(@meetingStatus=3)
	BEGIN
		if(@videoDuration<=30)
		BEGIN
		set @updatedPoints=75  
		END
		else if(@videoDuration<=90 and @videoDuration>30)
		BEGIN
		set @updatedPoints=150  
		END
		else 
		BEGIN
		set @updatedPoints=250  
		END
	END 
  set @candidateEmail=REPLACE(@candidateEmail,'Penalty','')  
  END  
   IF NOT EXISTS(select 1 from InterviewPoints where Interviewid = @interviewId and userid=@interviewerUserId and [Type]='Interview')        
    BEGIN        
     insert into InterviewPoints(Interviewid, userid, Points, [Type], Comment, [Status], [Date])        
     values (@interviewId, @interviewerUserId, @updatedPoints, 'Interview', @candidateEmail, 'Pending', getdate())        
    END        
   ELSE        
    BEGIN        
     update InterviewPoints set Points = @updatedPoints, [Date]=getdate(),Comment=@candidateEmail        
     where Interviewid = @interviewId and userid=@interviewerUserId and [Type]='Interview'        
    END        
  END      
      
 End        
 Else        
 Begin        
 IF EXISTS(select 1 from TransInterviewer where IRFId=@irfId and UserId=@interviewerUserId)        
  BEGIN        
   set @perSuccessRate = (select PerSuccessRate from TransInterviewer where IRFId=@irfId and UserId=@interviewerUserId)        
   set @updatedPoints= dbo.func_ComputeInterviewerPoints(@perSuccessRate, @videoDuration, @meetingStatus, @interviewStatus,@jobCountry)        
   if(@updatedPoints<0)  
   BEGIN  
   set @candidateEmail='Penalty '+@candidateEmail  
   END  
    if(@IsProctor=1)  
  BEGIN  
  if(@meetingStatus=4)
	BEGIN
		-- set for Selected or Rejected status with time check
		if(@videoDuration<=90)
		BEGIN
		set @updatedPoints=150  
		END
		else
		BEGIN
		set @updatedPoints=250  
		END
	END
	else if(@meetingStatus=1)
	BEGIN
	set @updatedPoints=50
	END
	else if(@meetingStatus=3)
	BEGIN
		if(@videoDuration<=30)
		BEGIN
		set @updatedPoints=75  
		END
		else if(@videoDuration<=90 and @videoDuration>30)
		BEGIN
		set @updatedPoints=150  
		END
		else 
		BEGIN
		set @updatedPoints=250  
		END
	END
  set @candidateEmail=REPLACE(@candidateEmail,'Penalty','')  
  END  
   IF NOT EXISTS(select 1 from InterviewPoints where Interviewid = @interviewId and userid=@interviewerUserId and [Type]='Interview')        
 BEGIN        
     insert into InterviewPoints(Interviewid, userid, Points, [Type], Comment, [Status], [Date])        
     values (@interviewId, @interviewerUserId, @updatedPoints, 'Interview', @candidateEmail, 'Pending', getdate())        
    END        
   ELSE        
    BEGIN        
     update InterviewPoints set Points = @updatedPoints, [Date]=getdate(),Comment=@candidateEmail        
     where Interviewid = @interviewId and userid=@interviewerUserId and [Type]='Interview'        
    END        
  END        
 ELSE IF EXISTS(select 1 from TransEmergencyInterviewer where IRFId=@irfId and UserId=@interviewerUserId)        
  BEGIN        
  set @perSuccessRate = (select PerSuccessRate from TransEmergencyInterviewer where IRFId=@irfId and UserId=@interviewerUserId)        
  set @updatedPoints= dbo.func_ComputeInterviewerPoints(@perSuccessRate, @videoDuration, @meetingStatus, @interviewStatus,@jobCountry)        
  if(@updatedPoints<0)  
   BEGIN  
   set @candidateEmail='Penalty '+@candidateEmail  
   END  
    if(@IsProctor=1)  
  BEGIN  
  if(@meetingStatus=4)
	BEGIN
		-- set for Selected or Rejected status with time check
		if(@videoDuration<=90)
		BEGIN
		set @updatedPoints=150  
		END
		else
		BEGIN
		set @updatedPoints=250  
		END
	END
	else if(@meetingStatus=1)
	BEGIN
	set @updatedPoints=50
	END
	else if(@meetingStatus=3)
	BEGIN
		if(@videoDuration<=30)
		BEGIN
		set @updatedPoints=75  
		END
		else if(@videoDuration<=90 and @videoDuration>30)
		BEGIN
		set @updatedPoints=150  
		END
		else 
		BEGIN
		set @updatedPoints=250  
		END
	END
  set @candidateEmail=REPLACE(@candidateEmail,'Penalty','')  
  END  
  IF NOT EXISTS(select 1 from InterviewPoints where Interviewid = @interviewId and userid=@interviewerUserId and [Type]='Interview')        
   BEGIN        
    insert into InterviewPoints(Interviewid, userid, Points, [Type], Comment, [Status], [Date])        
    values (@interviewId, @interviewerUserId, @updatedPoints, 'Interview', @candidateEmail, 'Pending', getdate())        
   END        
  ELSE        
   BEGIN        
    update InterviewPoints set Points = @updatedPoints, [Date]=getdate(),Comment=@candidateEmail        
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


