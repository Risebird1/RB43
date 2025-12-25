IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddOrUpdateWalletDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_AddOrUpdateWalletDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure usp_AddOrUpdateWalletDetails        
@Interviewid int,        
@CreatedBy varchar(200),        
@TransactionType varchar(50), --InterviewBilling,Manual        
@Transaction varchar(10),      
@Credit int =null      
AS        
BEGIN        
Declare @BillingType int,@Clientid int,@BillingOwnerId int,@IRFId int,@Debit int      
Declare @MeetingStatus int,@InterviewerUserid int,@RedoInterview bit,@Comment varchar(max),@CandidateEmail varchar(200),@Interviewstatus int,@InterviewDuration int      
Declare @CreatedOn Datetime=DateAdd(Minute,330,Getdate())      
Declare @BillingClientId int,@billingDetailsValid bit=0
Select @BillingType=cl.BillingType,@BillingOwnerId=IRF.BillingOwner,@Clientid=cl.id,@IRFId=IRF.id,@InterviewerUserid=ul.id,@RedoInterview=IsNull(im.RedoInterview,0),  
@CandidateEmail=im.candidateEmail  
from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId join Client cl on cl.Id=IRF.ClientId       
left join UserList ul on ul.EmailId=im.interviewer      
where im.id=@Interviewid  

Select @BillingClientId=id from ClientBillingDetails where Clientid=@Clientid
if(@BillingClientId>0 or @Clientid=177)
Begin
Set @billingDetailsValid=1
end
else
begin
Set @billingDetailsValid=0
end

if(@billingDetailsValid=1)  
begin  
 IF(@Transaction='Cr')        
 BEGIN        
 Insert into Wallet(BillingType,ClientId,BillingOwnerId,CreatedBy,Credit,Debit,CreatedOn,Comment,Balance,InterviewId,IRFId,TransactionType)      
 values(@BillingType,@Clientid,@BillingOwnerId,@CreatedBy,@Credit,0,@CreatedOn,'Credit',0,0,0,@TransactionType)      
 END        
 ELSE        
 BEGIN        
  Set @Comment='Debit: '+@CandidateEmail  
  Select @MeetingStatus=MeetingStatus,@Interviewstatus=InterviewStatus,@InterviewDuration=VideoDuration from InterviewComments where InterviewId=@Interviewid      
  if(@Clientid=177)
  begin
	  Select @Debit=BillingRate from InterviewerBillingRate where IRFId=@IRFId and Userid=IsNull(@InterviewerUserid,0)      
	  Set @Debit=ISNULL(@Debit,0)      
	  if(@MeetingStatus=1 or @MeetingStatus=9 or @MeetingStatus=3)      
	  begin      
	  Set @Debit=(@Debit*30/100)      
	  end      
  end
  else
  begin
--Apexon
      Declare @BillingMeetingStatus varchar(15)=''
	  if(@Interviewstatus=2 or @Interviewstatus=4)
	  begin
	  Set @BillingMeetingStatus='Select'
	  end
	  else if(@Interviewstatus=1 or @Interviewstatus=3 or @Interviewstatus=5)
	  begin
	  Set @BillingMeetingStatus='Reject'
	  end
      Select @Debit=(CASE WHEN cbd.[Value] LIKE '%/%' THEN (CAST(PARSENAME(REPLACE(cbd.[Value], '/', '.'), 2) AS FLOAT)/CAST(PARSENAME(REPLACE(cbd.[Value], '/', '.'), 1) AS FLOAT)) ELSE CAST(cbd.[Value] AS FLOAT) END) * cl.PerInterviewRate From ClientBillingDetails cbd join Client cl on cl.id=cbd.Clientid
	  where Clientid=@Clientid and MeetingStatus=@BillingMeetingStatus AND @InterviewDuration >= ISNULL(MinTime, 0) AND @InterviewDuration <= ISNULL(MaxTime, 2147483647)
	  Set @Debit=ISNULL(@Debit,0) 
	  if(@MeetingStatus=1 or @MeetingStatus=9 or @MeetingStatus=3)      
	  begin      
	  Set @Debit=0
	  end      
  end
  if(@MeetingStatus=2 or @MeetingStatus=5)      
  begin      
  Set @Debit=0  
  end      
  
  if(@RedoInterview=1)      
  begin      
  Set @Debit=0  
  Set @Comment='Redo Interview: '+@CandidateEmail  
  end      
   if exists(select top 1 id from Wallet where InterviewId = @Interviewid)      
   Begin      
   Update Wallet set BillingType=@BillingType,BillingOwnerId=@BillingOwnerId,Debit=@Debit where InterviewId=@Interviewid      
   end      
   else      
   begin    
    if(@Debit>0)  
    begin  
    Insert into Wallet(BillingType,ClientId,BillingOwnerId,CreatedBy,Credit,Debit,CreatedOn,Comment,Balance,InterviewId,IRFId,TransactionType)      
    values(@BillingType,@Clientid,@BillingOwnerId,@CreatedBy,0,@Debit,@CreatedOn,@Comment,0,@Interviewid,@IRFId,@TransactionType)      
    end  
   end      
 END        
end  
END   
GO