  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateConsentForm'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateConsentForm
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
   
     
Create Procedure usp_UpdateConsentForm  
@irfid int,  
@uploadedFileURL varchar(1000),  
@CreatedOn datetime  
AS  
BEGIN  
if exists(select top 1 id from ConsentForms where IRFId = @irfid)
begin
UPdate ConsentForms set FormURL=@uploadedFileURL where IRFId=@irfid
end
else
begin
Insert into ConsentForms (FormURL,IRFId,CreatedOn) values(@uploadedFileURL,@irfid,@CreatedOn)  
end
END  
GO