/****** Object:  StoredProcedure [dbo].[usp_GetProjectsByClientId]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetProjectsByClientId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetProjectsByClientId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetProjectsByClientId]
@ClientId int,
@IsDeleted bit,
@ExcludeProjectIds varchar(max) = NULL
AS
Begin

Select Project.id as ProjectId, Project.[Name] as ProjectName, Client.[Name] as Client, Client.id as ClientId
from Project
inner join Client on Project.ClientId = Client.id
where Project.ClientId = @ClientId
  and Project.IsDeleted = @IsDeleted
  and (@ExcludeProjectIds IS NULL OR @ExcludeProjectIds = '' OR Project.id NOT IN (SELECT value FROM string_split(@ExcludeProjectIds, ',')))

End

GO
