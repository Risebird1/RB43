IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveInterviewRating'))
BEGIN
    DROP PROCEDURE [dbo].[usp_SaveInterviewRating]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
  
Create Procedure usp_SaveInterviewRating  
@Emailid varchar(150),  
@Interviewid int,  
@StarRating int,  
@Comment varchar(400),
@ClientAction varchar(12)
AS  
BEGIN  
Declare @Userid int  
Declare @IRFId int
Select @Userid=id from Userlist where Emailid=@Emailid  
Select @IRFId=IRF.id from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId where im.id=@Interviewid
if EXISTS (Select 1 from InterviewRatings where Userid=@Userid and Interviewid=@Interviewid)  
Begin  
Update InterviewRatings set StarRating=@StarRating,Comment=@Comment,Addedon=GetDate() where Userid=@Userid and Interviewid=@Interviewid  
END  
Else   
Begin  
Insert into InterviewRatings (Interviewid,Userid,Comment,Addedon,StarRating)   
values(@Interviewid,@Userid,@Comment,GetDate(),@StarRating)  
END  
if EXISTS (Select 1 from TransInterviewer where Userid=@Userid and IRFId=@IRFId)  
BEGIN
if(@ClientAction='Rejected')
begin
Update TransInterviewer Set ClientAction=@ClientAction,IsActive=0 Where Userid=@Userid and IRFId=@IRFId
End
Else If(@ClientAction='Shortlisted')
BEGIN
Update TransInterviewer Set ClientAction=@ClientAction Where Userid=@Userid and IRFId=@IRFId
END
END
ELSE if EXISTS (Select 1 from TransEmergencyInterviewer where Userid=@Userid and IRFId=@IRFId) 
BEGIN
if(@ClientAction='Rejected')
begin
Update TransEmergencyInterviewer Set ClientAction=@ClientAction,IsActive=0 Where Userid=@Userid and IRFId=@IRFId
End
Else If(@ClientAction='Shortlisted')
BEGIN
Update TransEmergencyInterviewer Set ClientAction=@ClientAction Where Userid=@Userid and IRFId=@IRFId
END
END
END  
  

GO