/****** Object:  StoredProcedure [dbo].[usp_GetClientCandidatesCurrentStatus]    Script Date: 10-08-2020 6.18.17 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClientCandidatesCurrentStatus'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientCandidatesCurrentStatus
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetClientCandidatesCurrentStatus]  
@emailId varchar(100)
AS  

Begin 

declare @profileId int, @roleId int, @userId int;
	set @profileId = (select id from [Profile] where [Name]='Client')
	set @userId  = (select id from UserList where EmailId=@emailId and idProfile=@profileId)

	Begin
		
		declare @CandidateStatusGrouped table (IRFId int, CandidateId int, Status int, StatusValue varchar(75), Comments varchar(max))
				insert into @CandidateStatusGrouped 
				select IRFId, CandidateId, [Status], StatusValue, Comments from CandidateStatus cs join CandidateStatusOptionsMst csom on cs.[Status] = csom.id where cs.id in (select max(id) from CandidateStatus group by IRFId, CandidateId)
		
		select csg.* from @CandidateStatusGrouped csg join TransClientUser tcu on tcu.IRFId = csg.IRFId 
		where tcu.ClientUserId = @userId


	End

End
GO


