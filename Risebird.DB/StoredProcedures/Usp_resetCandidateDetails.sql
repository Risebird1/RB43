
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('Usp_resetCandidateDetails'))
BEGIN
    DROP PROCEDURE [dbo].[Usp_resetCandidateDetails]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
  

Create Procedure Usp_resetCandidateDetails  
@Clientjdid int,  
@candidateEmailid varchar(150),
@emailid varchar(150)
AS  
BEGIN  
DECLARE @interviewid int  
DECLARE @Clientid int  
DECLARE @Debit int  
Declare @Balance int
Declare @userid int
Declare @Comment varchar(200)
Select @userid=id from UserList where Emailid=@emailid
Select @interviewid=im.id from InterviewMeetings im join CandidateJDScore cj on cj.IRFID = im.IRFID where im.candidateEmail=@candidateEmailid  
Select @Clientid=Clientid from Clientjd where id=@Clientjdid  
  
Delete from SuitableCandidate where id in (Select sc.id from SuitableCandidate sc Join ClientUser cu on sc.Userid=cu.Userid Where Interviewid=@interviewid and Status='Interest Accepted' and cu.Clientid=@Clientid)  
Delete from RiseTubeVedioShortList where id in(Select sc.id from RiseTubeVedioShortList sc Join ClientUser cu on sc.Userid=cu.Userid Where Interviewid=@interviewid and cu.Clientid=@Clientid)  
Select @Balance=Balance from Client where id=@Clientid  
Select @Debit=Debit from clientWallet where Clientid=@Clientid and comment like '%'+@candidateEmailid+'%'  
--Delete from clientWallet where Clientid=@Clientid and comment like '%'+@candidateEmailid+'%'  
Set @Balance=ISNull(@Balance,0)+IsNull(@Debit,0)  
if(ISNull(@Debit,0)>0)
Begin
set @Comment='Revert points for interviewid: '+CONVERT(varchar(10),@interviewid)
Insert Into ClientWallet values(@Clientid,@userid,@Debit,0,Getdate(),@Comment,@Balance)
Update Client Set Balance=@Balance where id=@Clientid  
END
END  
  
  
  
  


  


  

Go

