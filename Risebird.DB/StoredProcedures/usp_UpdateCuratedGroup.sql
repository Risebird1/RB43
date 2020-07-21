/****** Object:  StoredProcedure [dbo].[usp_UpdateCuratedGroup]    Script Date: 13-07-2020 10.44.35 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateCuratedGroup'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateCuratedGroup
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




CREATE PROCEDURE [dbo].[usp_UpdateCuratedGroup]

@id int,
@Name varchar(100)

AS
Begin

update CuratedGroup set
[Name]=@Name
Where id =@id

delete from CuratedIRFMapping where CuratedGroupId=@id

End

GO


