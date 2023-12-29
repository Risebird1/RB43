IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetProxyCountDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetProxyCountDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetProxyCountDetails
@id int
As
Begin
Select * from ProxyReport where id=@id
End
GO