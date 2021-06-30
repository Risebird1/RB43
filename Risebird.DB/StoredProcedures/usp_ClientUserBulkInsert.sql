/****** Object:  StoredProcedure [dbo].[usp_ClientUserBulkInsert]    Script Date: 24-06-2020 1.32.58 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_ClientUserBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_ClientUserBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
Create PROCEDURE [dbo].[usp_ClientUserBulkInsert]  
(  
      @ClientUserBulkData AS [dbo].[TransClientUser_TableType] READONLY  
)  
AS  
BEGIN  
  
SET NOCOUNT ON;  
  DELETE from TransClientUser where IRFID in (Select IRFId From @ClientUserBulkData)
--This row performs bulk copy of object DataTable to Table in SQL  
INSERT INTO [dbo].[TransClientUser](  
 [IRFId] ,  
 [ClientUserId],  
 [ClientRoleId])  
   
  
SELECT cubd.*, ClientUser.RoleId as ClientRoleId FROM @ClientUserBulkData cubd left join ClientUser on cubd.ClientUserId= ClientUser.UserId  
  
  
END  
GO


