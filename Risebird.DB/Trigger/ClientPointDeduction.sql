-- ==============================================
-- Create dml trigger template Azure SQL Database 
-- ==============================================
-- Drop the dml trigger if it already exists
IF EXISTS(
  SELECT *
    FROM sys.triggers
   WHERE name = N'ClientPointDeduction'
     AND parent_class_desc = N'OBJECT_OR_COLUMN'
)
	DROP TRIGGER ClientPointDeduction
GO

CREATE TRIGGER ClientPointDeduction
   ON  ClientWallet
   After INSERT
AS 
BEGIN
	
	SET NOCOUNT ON;
	Declare @Clientid int
	Declare @Credit int
	Declare @Count int
	Select @Credit=i.Credit,@Clientid=i.Clientid from inserted i
	Select @Count=Count(*) from SuitableCandidate sc join clientuser cu on cu.userid=sc.Userid where cu.Clientid=@Clientid and sc.Pending=1
	if(@Count>0)
	Begin
	--Check balance and pending status
	Create Table #temp(Id int,clientjdid int,interviewid int);
		
	Insert into #temp Select Distinct sc.id,sc.Clientjdid,sc.Interviewid from SuitableCandidate sc join clientuser cu on cu.userid=sc.Userid where cu.Clientid=@Clientid and sc.Pending=1
	Select @Count=Count(*) from #temp
	while @Count>0
	Begin
	Set @Count=@Count-1

	Declare @CandidateEmail varchar(150)
	Declare @Videoid int
	Declare @Comment varchar(400)
	Declare @points int
	Declare @Balance int
	Declare @Userid int
	Declare @Clientjdid int
	Declare @id int
	Select top 1 @Videoid=interviewid,@Clientjdid=clientjdid,@id=Id from #temp
	Delete from #temp where interviewid=@Videoid and clientjdid=@Clientjdid
	select @CandidateEmail=candidateEmail from InterviewMeetings where id=@Videoid              
	Set @Comment='For Candidate: '+@CandidateEmail              
	Select @Points=persuccessrate from ClientJd where id=@Clientjdid      
	Select @Balance=(sum(Credit)-sum(debit)) from ClientWallet where Clientid=@ClientId           
	Set @Balance=ISnull(@Balance,0)-ISnull(@Points,0)          
	if(@Balance>=0)  
	Begin  
	Insert Into ClientWallet values(@ClientId,@Userid,0,@Points,Getdate(),@Comment,@Balance)             
	Update Client Set Balance=@Balance where id=@ClientId   
	Update SuitableCandidate set Pending=0 where id=@id  
	END  
	Else  
	Begin  
	Update SuitableCandidate set Pending=1 where id=@id  
	END  

	END
	Drop table #temp
	END
	
    

END
GO



