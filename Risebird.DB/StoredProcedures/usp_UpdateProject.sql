/****** Object:  StoredProcedure [dbo].[usp_UpdateProject]    Script Date: 20-09-2019 12.09.20 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateProject'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateProject
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create PROCEDURE [dbo].[usp_UpdateProject]
@id int,
@Name varchar(200),
@Location varchar(200),
@ClientId varchar(200),
@EndClient varchar(200),
@ProjectOwnerId varchar(200),
@ProjectTitle varchar(200)
AS
Begin

update Project set
[Name]=@Name,
[Location]=@Location,
[ClientId]=@ClientId,
[EndClient]=@EndClient,
[ProjectownerId]=@ProjectOwnerId,
[ProjectTitle]=@ProjectTitle
Where id =@id

delete from ProjectIRFMapping where ProjectId=@id
End
GO


