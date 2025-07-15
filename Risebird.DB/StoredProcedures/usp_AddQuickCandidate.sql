/****** Object:  StoredProcedure [dbo].[usp_AddCandidate]    Script Date: 29-11-2019 4.27.37 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddQuickCandidate'))
BEGIN
    DROP PROCEDURE dbo.usp_AddQuickCandidate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
    
CREATE PROCEDURE [dbo].usp_AddQuickCandidate            
@IRFId int = null,            
@Name varchar(200),            
@EmailId varchar(200),            
@ResumeLink varchar(500),              
@Phone varchar(200),          
@AddedByUserId int,           
@date datetime,          
@ClientHREmail varchar(200) = null,          
@ProfileSource varchar(200) = null,         
@WhatsappNumber varchar(15),     
@AffindaResumeIdentifier varchar(75) = null,    
@jobId varchar(20) = null,    
@countryCode varchar(10) = null,    
@operationStatus varchar(200) out          
          
            
AS            
Begin     
 begin try    
  begin tran    
    
   declare  @ProfileId int, @addedByEmail varchar(100),@loginProfile varchar(100),@RowCountValue int;          
   set @ProfileId = (select id from [Profile] where [Name]='Candidate')           
   set @addedByEmail= (select EmailId from UserList where id=@AddedByUserId) 
   Set @loginProfile=(Select ip.Name From UserList ul join Profile ip on ip.id=ul.idProfile where ul.id=@AddedByUserId)
   Set @RowCountValue=(Select Count(*) from TransClientUser tcu join UserList ul on ul.id=tcu.ClientUserId where ul.id=@AddedByUserId and tcu.IRFId=@IRFId and tcu.IsActive=1 and ul.IsActive=1)
   
   if(@IRFId is null and @jobId is not null and @countryCode is not null)    
   Begin    
    declare @locationType varchar(25), @countryType int=1    
    set @locationType = (select [Location] from Country_Classification where Code = @countryCode)    
    if(@locationType = 'Onsite')    
    Begin    
     set @countryType=2    
    End    
    
    set @IRFId = (select top 1.id from IRF where id in (select irfid from ProjectIRFMapping where ProjectId =     
    (select id from Project where ProjectUniqueid =@jobId) and IsAdded = 1) and JobCountry=@countryType)    
   End    
          
   IF EXISTS(select 1 from IRF where id=@IRFId and (Isnull(IsPaused,0)=1 or EndDate<Getdate()))          
   Begin          
   select @operationStatus='Skill Channel is not active'  
   End     
   Else IF(@RowCountValue=0 and @loginProfile='Client')  
	 Begin  
		select @operationStatus='You do not have this Skillchannel access'          
	End    
   Else IF EXISTS(select 1 from Candidate where IRFId=@IRFId and CandidateMasterId = (select id from CandidatesMaster where UserId= (select id from UserList where EmailId=@EmailId)))          
   Begin          
    select @operationStatus='Duplicate candidate'          
   End     
           
   Else IF Exists(select 1 from UserList where EmailId=@EmailId and idProfile != @ProfileId)          
   Begin          
    select @operationStatus='User exists with a different Profile'          
   End          
          
   Else IF NOT EXISTS (SELECT 1   FROM   UserList    WHERE  EmailId = @EmailId)            
    Begin            
     INSERT INTO [dbo].[UserList]            
     (idProfile,            
     FirstName,             
     LastName,            
     EmailId,            
     LastLogin,            
     LastLogout,            
     IsActive,            
     ImageURL,            
     MobileNumber,        
     WhatsappNumber,      
     uniqueid)            
             
     VALUES            
     (23,            
     @Name,            
     null,            
     @EmailId,            
     null,            
     null,            
     1,            
     null,            
     @Phone,        
     @WhatsappNumber,      
     NEWID())            
            
     declare            
     @UserId int = (select id from UserList where EmailId=@EmailId and idProfile=@ProfileId)            
            
     insert into CandidatesMaster ([UserId]) values (@UserId)            
            
     insert into Candidate            
     ([IRFId],            
     [ResumeLink],              
     [Phone],            
     [AddedByUserId],            
     [CandidateMasterId],          
     [ClientHRAttemptContact],          
     [ProfileSource],    
     [AffindaResumeIdentifier])            
     values          
     (@IRFId,            
     @ResumeLink,             
     @Phone,            
     @AddedByUserId,            
     (select id from CandidatesMaster where UserId=@UserId),          
     @ClientHREmail,          
     @ProfileSource,    
     @AffindaResumeIdentifier)            
      
     insert into CandidateStatus (IRFId, CandidateId, Comments, [Date], [Status], PerformedBy)           
     values (@IRFId, (select id from CandidatesMaster where UserId=@UserId), 'Added', GetDate(), 12, @addedByEmail)          
          
     insert into TransHREvaluatorIRF (IRFId, CandidateId) values (@IRFId, (select id from CandidatesMaster where UserId=@UserId))            
     select @operationStatus='Candidate imported successfully'          
            
    End            
            
   Else            
            
   Begin            
    declare            
            
    @CandidateUserId int = (select id from UserList where EmailId=@EmailId and idProfile=@ProfileId)            
            
            
    IF NOT EXISTS (SELECT 1   FROM   CandidatesMaster    WHERE  UserId = @CandidateUserId)            
    Begin            
     insert into CandidatesMaster ([UserId]) values (@CandidateUserId)            
    End            
    Update UserList Set WhatsappNumber=@WhatsappNumber where  EmailId = @EmailId         
            
    insert into Candidate            
    ([IRFId],            
    [ResumeLink],            
    [Phone],             
    [AddedByUserId],            
    [CandidateMasterId],          
    [ClientHRAttemptContact],          
    [ProfileSource],    
    [AffindaResumeIdentifier]    
    )            
    values(@IRFId,            
    @ResumeLink,            
    @Phone,            
    @AddedByUserId,            
    (select id from CandidatesMaster where UserId=@CandidateUserId),          
    @ClientHREmail,          
    @ProfileSource,    
    @AffindaResumeIdentifier    
    )              
            
    insert into CandidateStatus (IRFId, CandidateId, Comments, [Date], [Status], PerformedBy)           
    values (@IRFId, (select id from CandidatesMaster where UserId=@CandidateUserId), 'Added', GetDate(), 12, @addedByEmail)          
          
    insert into TransHREvaluatorIRF (IRFId, CandidateId) values (@IRFId, (select id from CandidatesMaster where  UserId=@CandidateUserId))          
    select @operationStatus='Candidate imported successfully'          
          
            
   End            
      
   IF(@jobId is not null and @countryCode is not null)    
   Begin    
    set @operationStatus = @operationStatus + '|' +  CAST(@IRFId AS VARCHAR(10))--@IRFId    
   End    
  commit tran    
 end try    
 Begin catch     
  rollback transaction    
  DECLARE @Message varchar(MAX) = ERROR_MESSAGE(),      
    @Severity int = ERROR_SEVERITY(),      
    @State smallint = ERROR_STATE()      
       
  RAISERROR (@Message, @Severity, @State)    
 end catch    
          
END   
GO