/****** Object:  StoredProcedure [dbo].[usp_GetProjectCluster]    Script Date: 23-03-2020 7.30.17 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetProjectCluster'))
BEGIN
    DROP PROCEDURE dbo.usp_GetProjectCluster
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetProjectCluster]

@ProjectId int

AS

Begin
select ProjectIRFMapping.*, Project.[Name] as ProjectName, IRF.IRFId as IRFName, IRF.ClientId, IRF.id as IRFId, IRF.JDTiltle, cl.[Name] as Client 
from ProjectIRFMapping 
inner join IRF on ProjectIRFMapping.IRFId = IRF.id 
left join Project on ProjectIRFMapping.ProjectId = Project.id 
left join Client cl on cl.id = irf.ClientId
where ProjectIRFMapping.ProjectId = @ProjectId and ProjectIRFMapping.IsAdded = 1

End

GO


