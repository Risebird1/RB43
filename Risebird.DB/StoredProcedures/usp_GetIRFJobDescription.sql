-- =============================================
-- SP Name:  dbo.usp_GetIRFJobDescription
-- =============================================
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetIRFJobDescription'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFJobDescription
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetIRFJobDescription]
@irfId varchar(200)
AS
Begin

 select JDTiltle from irf where IRFId = @irfId
 
End

GO



