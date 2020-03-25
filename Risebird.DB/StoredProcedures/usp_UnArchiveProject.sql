/****** Object:  StoredProcedure [dbo].[usp_UnArchiveProject]    Script Date: 25-03-2020 7.17.08 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UnArchiveProject'))
BEGIN
    DROP PROCEDURE dbo.usp_UnArchiveProject
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_UnArchiveProject]

@id int

AS
Begin

update Project set IsDeleted=0 where id = @id

End

GO


