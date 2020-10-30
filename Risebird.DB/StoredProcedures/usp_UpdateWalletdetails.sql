IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateWalletdetails'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateWalletdetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure usp_UpdateWalletdetails        
@Interviewid int        
AS        
BEGIN        
Declare @Userid int        
Declare @Videoid int        
Declare @ClientId int        
Declare @Count int        
Declare @CandidateEmail varchar(150)        
Declare @Comment varchar(max)        
Declare @Balance int        
Declare @Points int  
Declare @Clientjdid int
Select @Userid=Userid,@Videoid=Interviewid,@Clientjdid=ClientJdid from SuitableCandidate where id=@Interviewid        
Select @ClientId=ClientId from ClientUser where Userid=@Userid        
Select @Count=Count(*) from SuitableCandidate sc join ClientUser cu on sc.Userid=cu.Userid where sc.Interviewid=@Videoid and cu.ClientId=@ClientId and Status='Interest Accepted'        
if(@Count=0)        
Begin        
select @CandidateEmail=candidateEmail from InterviewMeetings where id=@Videoid        
Set @Comment='For Candidate: '+@CandidateEmail        
--Select @Points=PerSuccessRate from Client Where id=@ClientId      
Select @Points=persuccessrate from ClientJd where id=@Clientjdid
Select @Balance=(sum(Credit)-sum(debit)) from ClientWallet where Clientid=@ClientId     
Set @Balance=ISnull(@Balance,0)-ISnull(@Points,0)    
Insert Into ClientWallet values(@ClientId,@Userid,0,@Points,Getdate(),@Comment,@Balance)       
Update Client Set Balance=@Balance where id=@ClientId      
END        
END  
  
GO