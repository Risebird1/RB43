

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetSeniorHRClientIds'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSeniorHRClientIds
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetSeniorHRClientIds]
@id INT
AS
BEGIN
    SET NOCOUNT ON;

   SELECT Clientid FROM SeniorHR WHERE Userid = @id
END
GO
