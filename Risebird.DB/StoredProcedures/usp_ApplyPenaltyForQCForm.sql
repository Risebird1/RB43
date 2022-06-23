IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_ApplyPenaltyForQCForm'))
BEGIN
    DROP PROCEDURE dbo.usp_ApplyPenaltyForQCForm
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  

CREATE Procedure usp_ApplyPenaltyForQCForm  
@interviewid int,  
@interviewer varchar(150),  
@redflags int  
As  
BEGIN  
Declare @Userid int,  
@RowCount int,  
@CandidateEmail varchar(200),  
@Points int,  
@IRFId int,  
@PerSuccessRate int,  
@VideoDuration int,
@flagDuction int 
Select @flagDuction=QCFormdeductionRate From Settings 
Select @CandidateEmail=candidateEmail From InterviewMeetings where id=@interviewid  
Select @IRFId=IRF.id from InterviewMeetings im join IRF on im.IRFId=IRF.IRFId where im.id=@interviewid  
Select @VideoDuration=VideoDuration from InterviewComments where interviewid=@interviewid  
Select @Userid=id from UserList where EmailId=@interviewer  
Select @RowCount=Count(*) From InterviewPoints where Interviewid=@interviewid and Userid=@Userid and Type='Penalty QC'  
if(@redflags<=16)  
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
Update InterviewPoints set Points=@Points where Interviewid=@interviewid and Userid=@Userid and Type='Penalty QC'  
end  
else  
begin  
Insert into InterviewPoints(Interviewid,userid,Points,Type,Comment,Status,Date,InterviewerId,PaymentHistoryId) values  
(@interviewid,@Userid,@Points,'Penalty QC','QC Penalty '+@CandidateEmail,'Pending',Getdate(),0,0)  
end  
END  
  
GO
