/****** Object:  StoredProcedure [dbo].[usp_GetAllProjectsList]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllProjectsList'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllProjectsList
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetAllProjectsList]
AS
Begin

select * from Project

End

GO
