/****** Object:  StoredProcedure [dbo].[usp_GetIRFByClientId]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIRFByClientId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFByClientId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetIRFByClientId]
@ClientId int,
@IsDeleted bit,
@ExcludeIRFIds varchar(max) = NULL
AS
Begin

Select IRF.id as IRFId, IRF.IRFId as IRFName, Client.[Name] as Client, irf.JDTiltle, IRF.ClientId
from IRF
inner join Client on IRF.ClientId = Client.id
where irf.ClientId = @ClientId
  and IRF.IsDeleted = @IsDeleted
  and (@ExcludeIRFIds IS NULL OR @ExcludeIRFIds = '' OR irf.id NOT IN (SELECT value FROM string_split(@ExcludeIRFIds, ',')))

End

GO
