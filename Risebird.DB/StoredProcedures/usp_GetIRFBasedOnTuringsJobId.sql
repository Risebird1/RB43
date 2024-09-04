IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIRFBasedOnTuringsJobId'))
BEGIN
    DROP PROCEDURE usp_GetIRFBasedOnTuringsJobId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_GetIRFBasedOnTuringsJobId         
@jobId varchar(50),              
@countryType int,                      
@timeNow datetime, 
@planType int = null     
AS              
BEGIN  

select top 1.* from IRF where id in (select irfid from ProjectIRFMapping where ProjectId = 
(select id from Project where ProjectUniqueid =@jobId) and IsAdded = 1) and JobCountry=@countryType

                  
  
END       
GO