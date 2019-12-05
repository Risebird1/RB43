/****** Object:  StoredProcedure [dbo].[usp_UpdateProject]    Script Date: 05-12-2019 6.04.05 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateAccount'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateAccount
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UpdateAccount]
@id int,
@Name varchar(200),
@Location varchar(200),
@Client varchar(200),
@EndClient varchar(200),
@AccountOwnerId varchar(200)

AS
Begin

update Account set
[Name]=@Name,
[Location]=@Location,
[Client]=@Client,
[EndClient]=@EndClient,
[AccountOwnerId]=@AccountOwnerId
Where id =@id

delete from AccountProjectMapping where AccountId=@id

End

GO


