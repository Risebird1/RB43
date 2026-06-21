/****** Object:  StoredProcedure [dbo].[usp_GetActiveIRFList]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetActiveIRFList'))
BEGIN
    DROP PROCEDURE dbo.usp_GetActiveIRFList
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetActiveIRFList]
@IsDeleted bit
AS
Begin

Select IRF.id, IRF.IRFId, IRF.SkillSetsevaluate, IRF.Clientid, Client.[Name] as ClientCompName,
Case when irf.SkillSetsevaluate > 1 and ISNULL(irf.JDTitleTwo, '') != '' then irf.JDTiltle + ' and ' + irf.JDTitleTwo else irf.JDTiltle end as JDTiltle
from IRF
inner join Client on IRF.ClientId = Client.id
where IRF.IsDeleted = @IsDeleted

End

GO
