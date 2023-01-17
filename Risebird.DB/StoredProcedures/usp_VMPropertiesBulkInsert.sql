/****** Object:  StoredProcedure [dbo].[usp_VMPropertiesBulkInsert]    Script Date: 21-09-2020 12.05.45 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_VMPropertiesBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_VMPropertiesBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_VMPropertiesBulkInsert]  
(  
      @VMPropertiesBulkData AS [dbo].[VMProperties_TableType] READONLY  
)  
AS  
BEGIN  
  
SET NOCOUNT ON;  
  
--This row performs bulk copy of object DataTable to Table in SQL  
INSERT INTO [dbo].[VMProperties](  
 [Name] ,  
 [ResourceGroupName],  
 [Status],  
 [IsAdded],  
 [IsRemoved],  
 [IsDeleted],
 [shutdowntime],
 [deletetime],
 [VmLocation],
 [CreateDate])  
  
SELECT [Name],[ResourceGroupName],[Status],[IsAdded], [IsRemoved],[IsDeleted],[shutdowntime],[deletetime],[VmLocation],Getdate() FROM @VMPropertiesBulkData  
  
END  
GO


