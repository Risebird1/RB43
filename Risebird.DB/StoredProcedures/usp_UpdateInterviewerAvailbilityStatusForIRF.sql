IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateInterviewerAvailbilityStatusForIRF'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateInterviewerAvailbilityStatusForIRF
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_UpdateInterviewerAvailbilityStatusForIRF  
@IRFId int,  
@uniqueid varchar(50),  
@Status int,  
@timeNow datetime  
AS  
BEGIN  
Declare @Count int  
Declare @totalInterviewersCount int  
Declare @InterviewerId int  
Declare @UserId int  
  
declare @timeDifference int  

declare @sentAvailabilityMailOn datetime =  (select SendIntervieweravailbilityOn from IRF where id = @IRFId)
if(@sentAvailabilityMailOn is not null)
Begin
	set @timeDifference  = (select DATEDIFF(MINUTE, @sentAvailabilityMailOn, @timeNow))
End
Else
Begin
	set @timeDifference  = (select DATEDIFF(MINUTE, (select CreatedOn from IRF where id = @IRFId), @timeNow))  
End

Select @Count=Count(*) from TransInterviewer ti join UserList ul on ul.id=ti.Userid Where IRFId=@IRFId and ul.uniqueid = @uniqueid  
Select @totalInterviewersCount =Count(*) from TransInterviewer ti join UserList ul on ul.id=ti.Userid Where IRFId=@IRFId and ti.IsAvailable = 1  
  
if(@timeDifference < 60 and @totalInterviewersCount < 100)  
Begin  
 if(@Count=0)  
 BEGIN  
  if(@Status=1)  
   begin  
    Select @UserId=id from UserList where uniqueid=@uniqueid  
    Select @InterviewerId=id From InterviewerProfile where UserId=@UserId  
  
    Insert into TransInterviewer(IRFId,InterviewerId,UserId,IsActive,IsAvailable, PerSuccessRate)   
    values(@IRFId,@InterviewerId,@UserId,1,1, (select FlatRate from IRF where id=@IRFId))  
  
    update InterviewerProfile set  AvailabilityResponseDate = @timeNow, Isavailable = 1 where UserId = (Select id from UserList where uniqueid=@uniqueid)   
  
   end  
 END  
 ELSE  
 BEGIN  
  if(@Status=1)  
  begin  
   Update ti set ti.IsAvailable=1 from TransInterviewer ti join UserList ul on ul.id=ti.Userid Where IRFId=@IRFId and ul.uniqueid=@uniqueid  
   update InterviewerProfile set  AvailabilityResponseDate = @timeNow, Isavailable = 1 where UserId = (Select id from UserList where uniqueid=@uniqueid)   
  
  end  
  --else  
  --begin  
  -- Update ti set ti.IsActive=0,IsAvailable=0 from TransInterviewer ti join UserList ul on ul.id=ti.Userid Where IRFId=@IRFId and ul.uniqueid=@uniqueid  
  --end  
 END  
  
End  
  
if(@timeDifference < 60 and @Status != 1)  
Begin  
 Update ti set ti.IsActive=0,IsAvailable=0 from TransInterviewer ti join UserList ul on ul.id=ti.Userid Where IRFId=@IRFId and ul.uniqueid=@uniqueid  
   
 update InterviewerProfile set  AvailabilityResponseDate = @timeNow where UserId = (Select id from UserList where uniqueid=@uniqueid)   
  
End  
if(@timeDifference > 60 and @Status != 1)  
Begin  
 Update ti set ti.IsActive=0,IsAvailable=0 from TransInterviewer ti join UserList ul on ul.id=ti.Userid Where IRFId=@IRFId and ul.uniqueid=@uniqueid  
   
 update InterviewerProfile set  AvailabilityResponseDate = @timeNow where UserId = (Select id from UserList where uniqueid=@uniqueid)   
  
End  
  
  
--if(@totalInterviewersCount>=100 and @Status != 1)  
--Begin  
  
-- Update ti set ti.IsActive=0,IsAvailable=0 from TransInterviewer ti join UserList ul on ul.id=ti.Userid Where IRFId=@IRFId and ul.uniqueid=@uniqueid  
  
--End  
  
if(@Status = 4) --here it means the interviewer has asked to drop his/her profile, therefore set 4 in InterviwerProfile  
Begin  
 update InterviewerProfile set Isavailable = @Status, AvailabilityResponseDate = @timeNow where UserId = (Select id from UserList where uniqueid=@uniqueid)   
End  
   
END  
Go