IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetPendingCalendlyResourceUriUpdateIRF'))
BEGIN
    DROP PROCEDURE dbo.usp_GetPendingCalendlyResourceUriUpdateIRF
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
Create PROCEDURE [dbo].[usp_GetPendingCalendlyResourceUriUpdateIRF]        
      
        
AS        
Begin 
 select tcl.IRFId as id, IRF.CalendlyLink from TransCalendlyLinkAddedUpdated tcl join IRF on IRF.id = tcl.IRFId

End        
 

GO

