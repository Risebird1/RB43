IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddMCQSetShareCandidatePremium'))
BEGIN
    DROP PROCEDURE dbo.usp_AddMCQSetShareCandidatePremium
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_AddMCQSetShareCandidatePremium            
@PartitionKey varchar(50),            
@ApplyingCompany varchar(50),            
@ClientHR varchar(200),            
@EmailAddress varchar(200),            
@MCQId varchar(50),            
@MCQSet varchar(50),            
@Videorecording int,            
@status int,          
@roomId varchar(50),        
@Expiretime int,      
@WhoShared varchar(200),    
@ShareDate datetime
AS            
BEGIN           
Declare @MCQShareCount int        
Select @MCQShareCount=Count(*) From MCQShareListPremium where EmailAddress=@EmailAddress and MCQId=@MCQId        
if(@MCQShareCount>0)        
begin        
Update MCQShareListPremium set ShareDate=@ShareDate,Expiretime=@Expiretime,WhoShared=@WhoShared,roomId=@roomId where EmailAddress=@EmailAddress and MCQId=@MCQId         
end        
else        
begin        
Insert into MCQShareListPremium (PartitionKey,ApplyingCompany,ClientHR,EmailAddress,MCQId,MCQSet,Videorecording,status,ShareDate,roomId,Expiretime,WhoShared)             
values(@PartitionKey,@ApplyingCompany,@ClientHR,@EmailAddress,@MCQId,@MCQSet,@Videorecording,@status,@ShareDate,@roomId,@Expiretime,@WhoShared)            
end        
END
  Go