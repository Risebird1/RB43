IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddMCQSetShareCandidate'))
BEGIN
    DROP PROCEDURE dbo.usp_AddMCQSetShareCandidate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_AddMCQSetShareCandidate    
@PartitionKey varchar(50),    
@ApplyingCompany varchar(50),    
@ClientHR varchar(200),    
@EmailAddress varchar(200),    
@MCQId varchar(50),    
@MCQSet varchar(50),    
@Videorecording int,    
@status int,  
@roomId varchar(50)  
AS    
BEGIN    
Insert into MCQShareList (PartitionKey,ApplyingCompany,ClientHR,EmailAddress,MCQId,MCQSet,Videorecording,status,ShareDate,roomId)     
values(@PartitionKey,@ApplyingCompany,@ClientHR,@EmailAddress,@MCQId,@MCQSet,@Videorecording,@status,Getdate(),@roomId)    
END  
  Go