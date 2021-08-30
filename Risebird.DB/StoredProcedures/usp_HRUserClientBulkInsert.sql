IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_HRUserClientBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_HRUserClientBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_HRUserClientBulkInsert]  
(  
      @HRUserClientBulkData AS [dbo].[TransClientUser_TableType] READONLY  
)  
AS  
BEGIN  
  
SET NOCOUNT ON;  
  DELETE from SeniorHR where Userid in (Select Userid From @HRUserClientBulkData)
--This row performs bulk copy of object DataTable to Table in SQL  
INSERT INTO [dbo].[SeniorHR](  
 [Userid] ,  
 [Clientid])  
 Select * From @HRUserClientBulkData
 
END  
GO