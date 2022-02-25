
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_DisableInactiveUser'))
BEGIN
    DROP PROCEDURE dbo.usp_DisableInactiveUser
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_DisableInactiveUser]

@date datetime

AS
Begin

update UserList set IsActive=0 
from UserList ul join ClientUser cu on cu.UserId = ul.id 
join Client cl on cl.id = cu.ClientId
where datediff(day, cast(LastLogout as date), cast(@date as date))>180
and cl.IsPasswordReset = 1

END

GO


