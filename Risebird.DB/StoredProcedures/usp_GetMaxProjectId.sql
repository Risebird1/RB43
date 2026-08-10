/****** Object:  StoredProcedure [dbo].[usp_GetMaxProjectId]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetMaxProjectId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetMaxProjectId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetMaxProjectId]
AS
Begin

select top (1) id from Project order by id desc

End

GO
