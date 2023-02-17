IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetClientLogoByIRFId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientLogoByIRFId
END

SET ANSI_NULLS ON

SET QUOTED_IDENTIFIER ON

GO

CREATE PROCEDURE [dbo].[usp_GetClientLogoByIRFId]          
   @irfId int           
AS            
BEGIN            

	select top 1 cl.LogoLink from IRF join Client cl on cl.id = IRF.ClientId where irf.id = @irfId
           
END 