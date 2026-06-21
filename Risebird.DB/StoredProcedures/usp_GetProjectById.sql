/****** Object:  StoredProcedure [dbo].[usp_GetProjectById]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetProjectById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetProjectById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetProjectById]
@id int
AS
Begin

select * from Project where id = @id

End

GO
