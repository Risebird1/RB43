/****** Object:  StoredProcedure [dbo].[usp_GetAllProjects]    Script Date: 23-03-2020 6.52.30 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllProjects'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllProjects
END


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetAllProjects]  
@EmailId varchar(200),   
@Profile varchar(100)
AS  
Begin 

DECLARE @tbl_ProjectCluster TABLE (
    ProjectId INT,
    TotalIRF int 
);
INSERT INTO @tbl_ProjectCluster
select project.id as ProjectId, Count(Project.id) TotalIRF 
from Project left join ProjectIRFMapping pim on Project.id = pim.ProjectId 
group by Project.id

IF (@Profile = 'Client')  
	BEGIN  
		declare
		@ClientId int = (select ClientId from  ClientUser cu left join UserList ul on cu.UserId = ul.id  where ul.EmailId = @EmailId)

		select p.*, tpc.TotalIRF, cl.[Name] as Client, 1 as IsClientUser from Project p 
		left join @tbl_ProjectCluster tpc on  p.id = tpc.ProjectId
		left join Client cl on cl.id = p.ClientId
		where p.ClientId = @ClientId
	End  

Else
begin
		select p.*, tpc.TotalIRF, cl.[Name] as Client from Project p 
		left join @tbl_ProjectCluster tpc on  p.id = tpc.ProjectId
		left join Client cl on cl.id = p.ClientId

End
End
GO


