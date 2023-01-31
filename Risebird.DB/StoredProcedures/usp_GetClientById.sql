  
  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClientById'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetClientById]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
Create PROCEDURE [dbo].[usp_GetClientById]  
  
@id int  
  
AS  
Begin  
SELECT  * FROM   [dbo].[Client] WHERE  Id = @id  
End  
  
  