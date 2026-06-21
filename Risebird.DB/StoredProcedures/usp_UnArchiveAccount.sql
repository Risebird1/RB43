/****** Object:  StoredProcedure [dbo].[usp_UnArchiveAccount]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UnArchiveAccount'))
BEGIN
    DROP PROCEDURE dbo.usp_UnArchiveAccount
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UnArchiveAccount]
@id int
AS
Begin

update Account set IsDeleted = 0 where id = @id

End

GO
