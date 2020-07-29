/****** Object:  StoredProcedure [dbo].[usp_GetProgressOptions]    Script Date: 29-07-2020 10.48.48 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetProgressOptions'))
BEGIN
    DROP PROCEDURE dbo.usp_GetProgressOptions
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_GetProgressOptions]  
AS  
Begin 

select id, [Name] from IrfProgress


End

GO


