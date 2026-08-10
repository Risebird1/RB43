/****** Object:  StoredProcedure [dbo].[usp_GetAccountById]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAccountById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAccountById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetAccountById]
@id int
AS
Begin

select * from Account where id = @id

End

GO
