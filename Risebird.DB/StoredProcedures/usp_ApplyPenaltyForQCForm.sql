IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_ApplyPenaltyForQCForm'))
BEGIN
    DROP PROCEDURE dbo.usp_ApplyPenaltyForQCForm
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_ApplyPenaltyForQCForm    
@interviewid int,    
@interviewer varchar(150),    
@redflags int,
@VideoReward int
As    
BEGIN    
Declare @Userid int,    
@RowCount int,    
@CandidateEmail varchar(200),    
@Points int,    
@IRFId int,    
@PerSuccessRate int,    
@VideoDuration int,  
@flagDuction int,
@IsnewCreated bit
Select @flagDuction=QCFormdeductionRate From Settings   
Select @CandidateEmail=candidateEmail From InterviewMeetings where id=@interviewid    
Select @IRFId=IRF.id from InterviewMeetings im join IRF on im.IRFId=IRF.IRFId where im.id=@interviewid    
Select @VideoDuration=VideoDuration from InterviewComments where interviewid=@interviewid    
Select @Userid=id from UserList where EmailId=@interviewer   
Select @IsnewCreated=IRF.IsNewSkillChannel from InterviewMeetings im join IRF on im.IRFId=IRF.IRFId where im.id=@interviewid    
Select @RowCount=Count(*) From InterviewPoints where Interviewid=@interviewid and Userid=@Userid and Type='Interview'    
if(@redflags=0 and @IsnewCreated=1)
begin
set @Points=0
end
else if(@redflags<=16)    
begin    
set @Points=-(@flagDuction*@redflags)    
end    
else    
begin    
Select @PerSuccessRate=PerSuccessRate From TransInterviewer where IRFId=@IRFId and UserId=@Userid    
if(@VideoDuration>60)    
begin    
set @Points=-(@PerSuccessRate)    
end    
else    
begin    
set @Points=-(@PerSuccessRate*@VideoDuration/60)    
end    
end    
if(@RowCount>0)    
begin    
Update InterviewPoints set QCPoints=@Points,VideoReward=@VideoReward where Interviewid=@interviewid and Userid=@Userid and Type='Interview'    
end    
else    
begin    
Insert into InterviewPoints(Interviewid,userid,Points,Type,Comment,Status,Date,InterviewerId,PaymentHistoryId,QCPoints,VideoReward) values    
(@interviewid,@Userid,0,'Interview',@CandidateEmail,'Pending',Getdate(),0,0,@Points,@VideoReward)    
end    
END 
GO
