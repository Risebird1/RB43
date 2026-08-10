/****** Object:  StoredProcedure [dbo].[usp_ArchiveAccount]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_ArchiveAccount'))
BEGIN
    DROP PROCEDURE dbo.usp_ArchiveAccount
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_ArchiveAccount]
@id int
AS
Begin

update Account set IsDeleted = 1 where id = @id

End

GO
