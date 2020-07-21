/****** Object:  StoredProcedure [dbo].[usp_GetAllCuratedGroups]    Script Date: 13-07-2020 10.39.27 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllCuratedGroups'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllCuratedGroups 
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



CREATE PROCEDURE [dbo].[usp_GetAllCuratedGroups]  
AS  
Begin 
select cg.id, cg.[Name], (select count(id) from CuratedIRFMapping where CuratedGroupId = cg.id) as TotalSkillsChannels from CuratedGroup cg 

End
GO


