IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_applyInterviewerpenalty'))
BEGIN
    DROP PROCEDURE dbo.usp_applyInterviewerpenalty
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
Create Procedure usp_applyInterviewerpenalty  
@id int,  
@points int,  
@interviewer varchar(150)  
AS  
BEGIN  
Declare @Userid int  
Declare @CandidateEmail Varchar(150)  
Declare @IsProctor bit   
Select @Userid=id From UserList where Emailid =@interviewer  
Select @CandidateEmail=candidateEmail From InterviewMeetings where id=@id  
Select @IsProctor=ISNULL(ca.IsProctor,0) from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId join Userlist ul on im.candidateEmail=ul.EmailId   
join CandidatesMaster cm on ul.id=cm.UserId join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id  where im.id=@id                   
  
 if(@IsProctor=1)  
  BEGIN  
  set @points=0  
  END  
if exists (Select 1 from InterviewPoints where Interviewid=@id and userid=@Userid and type='Interview')  
BEGIN  
Update InterviewPoints set Points=@points where Interviewid=@id and userid=@Userid and type='Interview'  
END  
ELSE  
BEGIN  
Insert into InterviewPoints(Interviewid,userid,Points,Type,Comment,Status,Date,InterviewerId) values  
(@id,@Userid,@points,'Interview','Penalty '+@CandidateEmail,'Pending',Getdate(),0)  
END  
END  
GO

