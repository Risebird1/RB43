/****** Object:  StoredProcedure [dbo].[usp_ArchiveProject]    Script Date: 25-03-2020 7.17.52 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_ArchiveProject'))
BEGIN
    DROP PROCEDURE dbo.usp_ArchiveProject
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_ArchiveProject]

@id int

AS
Begin

update Project set IsDeleted=1 where id=@id
End

GO


