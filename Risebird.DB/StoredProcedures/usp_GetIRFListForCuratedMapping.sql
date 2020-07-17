/****** Object:  StoredProcedure [dbo].[usp_GetIRFListForCuratedMapping]    Script Date: 13-07-2020 10.39.51 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIRFListForCuratedMapping'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFListForCuratedMapping 
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



CREATE PROCEDURE [dbo].[usp_GetIRFListForCuratedMapping]  
AS  
Begin 
select id as IRFId, IRFId as IRFName, 0 as IsAdded, CASE WHEN CHARINDEX('|', JDTiltle) != 0 THEN (SUBSTRING(JDTiltle,0, (CHARINDEX('|', JDTiltle)))) ELSE JDTiltle END as PrimarySkills  from IRF

End
GO


