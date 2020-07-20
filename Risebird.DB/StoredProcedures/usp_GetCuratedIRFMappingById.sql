/****** Object:  StoredProcedure [dbo].[usp_GetCuratedIRFMappingById]    Script Date: 13-07-2020 10.44.20 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCuratedIRFMappingById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCuratedIRFMappingById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




CREATE PROCEDURE [dbo].[usp_GetCuratedIRFMappingById]  
@CuratedGroupId int
AS  
Begin 
select c.IRFId, c.IRFName,c.PrimarySkills,
 @CuratedGroupId as CuratedGroupId,
 (select [Name] from CuratedGroup where id = @CuratedGroupId) as CuratedGroupName,
CASE WHEN c.IsAdded != 0 THEN c.IsAdded ELSE 0 END as IsAdded
from (select irf.id as IRFId,irf.IRFId as IRFName,
CASE WHEN CHARINDEX('|', JDTiltle) != 0 THEN (SUBSTRING(JDTiltle,0, (CHARINDEX('|', JDTiltle)))) ELSE JDTiltle END as PrimarySkills,
b.CuratedGroupId, b.[Name], b.IsAdded from IRF left join (select cim.CuratedGroupId, cg.[Name], cim.IRFId, cim.IsAdded from CuratedGroup cg join CuratedIRFMapping cim on cim.CuratedGroupId = cg.id
where cg.id=@CuratedGroupId) b on b.IRFId = irf.id) c
End
GO


