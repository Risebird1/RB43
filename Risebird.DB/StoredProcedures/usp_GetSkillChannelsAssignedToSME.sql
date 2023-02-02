/****** Object:  StoredProcedure [dbo].[usp_GetSkillChannelsAssignedToSME]   ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetSkillChannelsAssignedToSME'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSkillChannelsAssignedToSME
END


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetSkillChannelsAssignedToSME]  
@loggedInInterviewer varchar(100) 
AS  
Begin 

declare @profileType varchar(50) = (select pr.[Name] from UserList ul join [Profile] pr on pr.id = ul.idProfile where ul.EmailId = @loggedInInterviewer )

IF (@profileType = 'Interviewer')  
	BEGIN  

		declare @interviewerProfileId int = (select ipr.id from InterviewerProfile ipr join UserList ul on ul.id = ipr.UserId where ul.EmailId = @loggedInInterviewer)

		select scs.InterviewerProfileId ,irf.IRFId as SkillChannelName, irf.id as IRFId, irf.JDTiltle as JDTitle, cl.[Name] as Client, 
		irf.PrimarySkillInvite as PrimarySkill, scs.PerformedAt as AssignedOn, ul.EmailId as ChannelOwner 
		from IRF join SkillChannelSMEs scs on scs.IRFId = irf.id 
		join Client cl on cl.id = irf.ClientId
		join UserList ul on ul.id = irf.RisebirdHr
		where (scs.IsDeleted is null or scs.IsDeleted =0) and scs.InterviewerProfileId = @interviewerProfileId and (irf.IsLocked is null or irf.IsLocked = 0)
		order by scs.PerformedAt desc

	End  


End
GO

