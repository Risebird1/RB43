/****** Object:  StoredProcedure [dbo].[usp_GetIRFById]    Script Date: 3/12/2021 7:59:59 AM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIRFById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFById
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



Create PROCEDURE [dbo].[usp_GetIRFById]

@IRFId int

AS
Begin

 select IRF.IRFId, IRF.JDTiltle, cl.[Name] as ClientCompName from IRF join Client cl on cl.id= irf.ClientId
 where IRF.id=@IRFId


End
Go











GO


