/****** Object:  StoredProcedure [dbo].[usp_GetIRFIdList]    Script Date: 25-03-2020 7.13.49 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIRFIdList'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFIdList
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetIRFIdList]

@id int

AS
Begin

select IRFId from ProjectIRFMapping where ProjectId=@id

End

GO


