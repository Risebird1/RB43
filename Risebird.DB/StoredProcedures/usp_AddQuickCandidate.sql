/****** Object:  StoredProcedure [dbo].[usp_AddCandidate]    Script Date: 29-11-2019 4.27.37 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddQuickCandidate'))
BEGIN
    DROP PROCEDURE dbo.usp_AddQuickCandidate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].usp_AddQuickCandidate      
@IRFId int,      
@Name varchar(200),      
@EmailId varchar(200),      
@ResumeLink varchar(500),        
@Phone varchar(200),    
@AddedByUserId int,     
@date datetime,    
@ClientHREmail varchar(200),    
@ProfileSource varchar(200),   
@WhatsappNumber varchar(15),
@uniqueid varchar(50),
@operationStatus varchar(200) out    
    
      
AS      
Begin      
 declare  @ProfileId int, @addedByEmail varchar(100);    
 set @ProfileId = (select id from [Profile] where [Name]='Candidate')     
 set @addedByEmail= (select EmailId from UserList where id=@AddedByUserId)    
    
IF EXISTS(select 1 from Candidate where IRFId=@IRFId and CandidateMasterId = (select id from CandidatesMaster where UserId= (select id from UserList where EmailId=@EmailId)))    
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
  @uniqueid)      
      
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
  [ProfileSource])      
  values    
  (@IRFId,      
  @ResumeLink,       
  @Phone,      
  @AddedByUserId,      
  (select id from CandidatesMaster where UserId=@UserId),    
  @ClientHREmail,    
  @ProfileSource)      
    
  insert into CandidateStatus (IRFId, CandidateId, Comments, [Date], [Status], PerformedBy)     
  values (@IRFId, (select id from CandidatesMaster where UserId=@UserId), 'Added', @date, 12, @addedByEmail)    
    
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
 [ProfileSource]    
 )      
 values(@IRFId,      
 @ResumeLink,      
 @Phone,      
 @AddedByUserId,      
 (select id from CandidatesMaster where UserId=@CandidateUserId),    
 @ClientHREmail,    
 @ProfileSource    
 )        
      
 insert into CandidateStatus (IRFId, CandidateId, Comments, [Date], [Status], PerformedBy)     
 values (@IRFId, (select id from CandidatesMaster where UserId=@CandidateUserId), 'Added', @date, 12, @addedByEmail)    
    
 insert into TransHREvaluatorIRF (IRFId, CandidateId) values (@IRFId, (select id from CandidatesMaster where  UserId=@CandidateUserId))    
 select @operationStatus='Candidate imported successfully'    
    
      
End      
      
END    
GO
    
    
  
  
  
  


GO


