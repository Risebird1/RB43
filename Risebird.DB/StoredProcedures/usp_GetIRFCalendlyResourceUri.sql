IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetIRFCalendlyResourceUri'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFCalendlyResourceUri
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetIRFCalendlyResourceUri]          
@irfId int          
AS          
Begin          

select ResourceUri from SkillChannelCalendlyDetails where IRFId =@irfId  

End 
GO
