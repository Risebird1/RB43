/****** Object:  StoredProcedure [dbo].[usp_GetAllClients]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllClients'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllClients
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
  
CREATE PROCEDURE [dbo].[usp_GetAllClients]  

AS  
Begin  

select id, [Name] from Client

End  
  
  
GO


