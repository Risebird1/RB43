/****** Object:  StoredProcedure [dbo].[usp_GetProjectbyClientId]    Script Date: 24-03-2020 8.21.23 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetProjectbyClientId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetProjectbyClientId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetProjectbyClientId]

@ClientId int

AS
Begin

select p.id as ProjectId, p.[Name] as ProjectName, cl.[Name] as Client 
from Project p join Client cl on cl.id = p.ClientId where ClientId = @ClientId and p.IsDeleted=0

End

GO


