/****** Object:  StoredProcedure [dbo].[usp_GetUserList]    Script Date: 28-09-2020 12.02.13 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetUserList'))
BEGIN
    DROP PROCEDURE dbo.usp_GetUserList
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetUserList]
@vmId int,
@profileId int
AS  
Begin 
declare @commonTable table (VmId int, UserId int, IsAdded bit);
		insert into @commonTable 
		select VmId, UserId,  1 as IsAdded from TransVMUser where Vmid=@vmId


select ul.id as UserId, EmailId, pr.[Name] as profileName, tvu.IsAdded, @vmId as VmId from UserList ul join [Profile] pr on ul.idProfile = pr.id
left join @commonTable tvu on tvu.UserId = ul.id
where idProfile = @profileId
End
GO

