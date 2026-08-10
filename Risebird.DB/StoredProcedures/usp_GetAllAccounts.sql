/****** Object:  StoredProcedure [dbo].[usp_GetAllAccounts]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllAccounts'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllAccounts
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetAllAccounts]
AS
Begin

select Account.*, a.TotalProjects
from Account
left join (
    select Count(AccountId) as TotalProjects, AccountId
    from AccountProjectMapping
    group by AccountId
) a on Account.id = a.AccountId
where Account.IsDeleted = 0

End

GO
