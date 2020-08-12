/****** Object:  StoredProcedure [dbo].[usp_GetClientCandidatesCount]    Script Date: 10-08-2020 6.18.17 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClientCandidatesCount'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientCandidatesCount
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetClientCandidatesCount]  
@emailId varchar(100)
AS  

Begin 

declare @profileId int, @roleId int, @userId int;
	set @profileId = (select id from [Profile] where [Name]='Client')
	set @userId  = (select id from UserList where EmailId=@emailId and idProfile=@profileId)
	set @roleId  = (select RoleId from ClientUser where UserId=@userId)

if(@roleId=1)
	Begin
		--declare @emailId varchar(100);
		--set @emailId = 'ramanaidu.m@mphasis.com'
		select count(distinct candidateEmail) as TotalCandidates from InterviewMeetings im where im.ClientHrContact=@emailId and im.confirmationStatus=1
	End
else if(@roleId=4)
	Begin
		--declare @emailId varchar(100);
		--set @emailId = 'shweta_kadechkar@risebird.io'
		select count(distinct candidateEmail) as TotalCandidates from InterviewMeetings im where im.staffingAgency=@emailId and im.confirmationStatus=1
	End
	
else
	Begin
		select count(distinct cm.UserId) as TotalCandidates from TransClientUser tcu join irf on irf.id= tcu.IRFId
		join Candidate ca on ca.IRFId=irf.id join CandidatesMaster cm on cm.id=ca.CandidateMasterId
		where tcu.ClientUserId=@userId
	End


End

GO


