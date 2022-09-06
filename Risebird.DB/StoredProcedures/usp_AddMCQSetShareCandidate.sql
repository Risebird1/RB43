IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddMCQSetShareCandidate'))
BEGIN
    DROP PROCEDURE dbo.usp_AddMCQSetShareCandidate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_AddMCQSetShareCandidate          
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
Select @MCQShareCount=Count(*) From MCQShareList where EmailAddress=@EmailAddress and MCQId=@MCQId      
if(@MCQShareCount>0)      
begin      
Update MCQShareList set ShareDate=@ShareDate,Expiretime=@Expiretime,WhoShared=@WhoShared,roomId=@roomId where EmailAddress=@EmailAddress and MCQId=@MCQId       
end      
else      
begin      
Insert into MCQShareList (PartitionKey,ApplyingCompany,ClientHR,EmailAddress,MCQId,MCQSet,Videorecording,status,ShareDate,roomId,Expiretime,WhoShared)           
values(@PartitionKey,@ApplyingCompany,@ClientHR,@EmailAddress,@MCQId,@MCQSet,@Videorecording,@status,@ShareDate,@roomId,@Expiretime,@WhoShared)          
end      
END
  Go