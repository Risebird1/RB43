/****** Object:  StoredProcedure [dbo].[usp_GetAccountCluster]    Script Date: 24-03-2020 8.21.48 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAccountCluster'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAccountCluster
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetAccountCluster]

@AccountId int

AS
Begin

select apm.*, Account.[Name] as AccountName, Project.[Name] as ProjectName, Project.ClientId, Client.[Name] as Client
from AccountProjectMapping apm inner join Project on apm.ProjectId = Project.id 
left join Account on apm.AccountId = Account.id 
left join Client on Client.id = Project.ClientId
where apm.AccountId = @AccountId and apm.IsAdded = 1

End

GO


