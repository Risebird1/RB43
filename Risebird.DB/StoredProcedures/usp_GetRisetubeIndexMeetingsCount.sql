/****** Object:  StoredProcedure [dbo].[usp_GetRisetubeIndexMeetingsCount]    Script Date: 12-11-2020 6.24.13 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetRisetubeIndexMeetingsCount'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRisetubeIndexMeetingsCount
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetRisetubeIndexMeetingsCount]  
@searchValueParam varchar(max) = null,
@clientIdParam int,
@EmailIdParam varchar(100)

AS  
Begin 
declare @totalResults int, @searchValue varchar(max), @Emailid varchar(100), @clientid int
		
		set @searchValue=@searchValueParam
		set @Emailid=@EmailIdParam
		set @clientid=@clientIdParam



if @clientid = 0
Begin
	CREATE TABLE #PatchVideoCountTbl (InterviewId int, VideoCount int)

	insert into #PatchVideoCountTbl
	select vqd.Interviewid, count(vqd.id) from VideoQuestionDetails vqd
	join InterviewComments ic on ic.InterviewId = vqd.Interviewid
	where NULLIF(vqd.VideoLink, '') IS NOT NULL
	group by vqd.Interviewid

	--CREATE TABLE #TechSkillScoreTbl (InterviewId int, WeightageActual float)

	--insert into #TechSkillScoreTbl
	--select tsr.Interviewid, sum(tsr.WeightageActual) from TechSkillReport tsr
	--join InterviewComments ic on ic.InterviewId = tsr.Interviewid
	--group by tsr.Interviewid

declare @idProfile int
set @idProfile=(select idProfile from UserList where EmailId=@Emailid)

if(@idProfile=2)
	Begin
		if(@searchValue is not null)
			Begin
				set @totalResults=(select count(a.id) from (select im.id from InterviewMeetings im 
						inner join IRF on im.IRFId = IRF.IRFId
						left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId      
						left join UserList ul on ul.EmailId = im.candidateEmail      
						join CandidatesMaster cm on ul.id = cm.UserId      
						join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id      
						left join QCFeedbackReport QCF on im.id=QCF.InterviewId      
						inner join client cl on IRF.ClientID=cl.id
						join TransHR th on th.IRFId = irf.id
						join HR on HR.id = th.HRId
						join UserList ulh on ulh.id = HR.UserId
						left join #PatchVideoCountTbl as patchvideoCount on patchvideoCount.InterviewId = ic.InterviewId
					    --left join #TechSkillScoreTbl as Techscore on Techscore.InterviewId = ic.InterviewId
						--outer apply (select count(*) as MeetingCount from VideoQuestionDetails where Interviewid=ic.InterviewId and NULLIF(VideoLink, '') IS NOT NULL)patchvideoCount
						--outer apply (select sum(WeightageActual) as TechSkillScore from TechSkillReport where Interviewid=ic.InterviewId )Techscore
						where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and (im.candidateName like ('%' +@searchValue +'%') or irf.JDTiltle like ('%' +@searchValue +'%')) and ulh.EmailId = @Emailid
						group by im.id,im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,      
						im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,      
						im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,      
						im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.JoiningBiometryScore,       
						ca.Phone, ca.ResumeLink,IRF.id,IRF.JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,
						ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, 
						cm.id,ic.ThumbnailImage ,cl.CuratedLink , QCF.InterviewId ,ic.CandidateAvailable,
						patchvideoCount.VideoCount, ic.TotalTechScore, ic.ThumbnailTimeSeconds) a)

			End
		Else
			Begin
					set @totalResults=(select count(a.id) from (select im.id from InterviewMeetings im 
						inner join IRF on im.IRFId = IRF.IRFId        
						left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId      
						left join UserList ul on ul.EmailId = im.candidateEmail      
						join CandidatesMaster cm on ul.id = cm.UserId      
						join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id      
						left join QCFeedbackReport QCF on im.id=QCF.InterviewId      
						inner join client cl on IRF.ClientID=cl.id 
						join TransHR th on th.IRFId = irf.id
						join HR on HR.id = th.HRId
						join UserList ulh on ulh.id = HR.UserId
						left join #PatchVideoCountTbl as patchvideoCount on patchvideoCount.InterviewId = ic.InterviewId
				        --left join #TechSkillScoreTbl as Techscore on Techscore.InterviewId = ic.InterviewId
						--outer apply (select count(*) as MeetingCount from VideoQuestionDetails where Interviewid=ic.InterviewId and NULLIF(VideoLink, '') IS NOT NULL)patchvideoCount
						--outer apply (select sum(WeightageActual) as TechSkillScore from TechSkillReport where Interviewid=ic.InterviewId )Techscore
						where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and ulh.EmailId = @Emailid
						group by im.id,im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,      
						im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,      
						im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,      
						im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.JoiningBiometryScore,       
						ca.Phone, ca.ResumeLink,IRF.id,IRF.JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,
						ic.AadharFileLink,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, 
						ic.InterviewStatus,ic.MeetingStatus, cm.id,ic.ThumbnailImage ,cl.CuratedLink , 
						QCF.InterviewId ,ic.CandidateAvailable,patchvideoCount.VideoCount,
						ic.TotalTechScore, ic.ThumbnailTimeSeconds) a)
			End

	End

else

	Begin
		if(@searchValue is not null)
			Begin
				set @totalResults=(select count(a.id) from (select im.id from InterviewMeetings im 
						inner join IRF on im.IRFId = IRF.IRFId
						left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId      
						left join UserList ul on ul.EmailId = im.candidateEmail      
						join CandidatesMaster cm on ul.id = cm.UserId      
						join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id      
						left join QCFeedbackReport QCF on im.id=QCF.InterviewId      
						inner join client cl on IRF.ClientID=cl.id
						left join #PatchVideoCountTbl as patchvideoCount on patchvideoCount.InterviewId = ic.InterviewId
						--left join #TechSkillScoreTbl as Techscore on Techscore.InterviewId = ic.InterviewId
						--outer apply (select count(*) as MeetingCount from VideoQuestionDetails where Interviewid=ic.InterviewId and NULLIF(VideoLink, '') IS NOT NULL)patchvideoCount
						--outer apply (select sum(WeightageActual) as TechSkillScore from TechSkillReport where Interviewid=ic.InterviewId )Techscore
						where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and (im.candidateName like ('%' +@searchValue +'%') or irf.JDTiltle like ('%' +@searchValue +'%')) 
						group by im.id,im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,      
						im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,      
						im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,      
						im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.JoiningBiometryScore,       
						ca.Phone, ca.ResumeLink,IRF.id,IRF.JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,
						ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id,ic.ThumbnailImage ,
						cl.CuratedLink , QCF.InterviewId ,ic.CandidateAvailable,
						patchvideoCount.VideoCount, ic.TotalTechScore, ic.ThumbnailTimeSeconds) a)

			End
		Else
			Begin
				 set @totalResults=(select count(a.id) from (select im.id from InterviewMeetings im 
						inner join IRF on im.IRFId = IRF.IRFId        
						left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId      
						left join UserList ul on ul.EmailId = im.candidateEmail      
						join CandidatesMaster cm on ul.id = cm.UserId      
						join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id      
						left join QCFeedbackReport QCF on im.id=QCF.InterviewId      
						inner join client cl on IRF.ClientID=cl.id 
						left join #PatchVideoCountTbl as patchvideoCount on patchvideoCount.InterviewId = ic.InterviewId
						--left join #TechSkillScoreTbl as Techscore on Techscore.InterviewId = ic.InterviewId
						--outer apply (select count(*) as MeetingCount from VideoQuestionDetails where Interviewid=ic.InterviewId and NULLIF(VideoLink, '') IS NOT NULL)patchvideoCount
						--outer apply (select sum(WeightageActual) as TechSkillScore from TechSkillReport where Interviewid=ic.InterviewId )Techscore
						where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null
						group by im.id,im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,      
						im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,      
						im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,      
						im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.JoiningBiometryScore,       
						ca.Phone, ca.ResumeLink,IRF.id,IRF.JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,
						ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id,
						ic.ThumbnailImage ,cl.CuratedLink , QCF.InterviewId ,ic.CandidateAvailable,patchvideoCount.VideoCount,
						ic.TotalTechScore, ic.ThumbnailTimeSeconds) a)
			End

	End

--drop table #TechSkillScoreTbl
drop table #PatchVideoCountTbl

End



Else
	Begin

		--CREATE TABLE #tblTechSkill (InterviewId int, TechSkillScore float)

		--insert into #tblTechSkill
		--select tsr.Interviewid, sum(tsr.WeightageActual) from TechSkillReport tsr
		--group by tsr.Interviewid

		declare @tblPatchCount table (InterviewId int, MeetingPatchCount int);
			insert into @tblPatchCount
			select InterviewId, count(InterviewId)  from VideoQuestionDetails where NULLIF(VideoLink, '') IS NOT NULL group by InterviewId

		if(@searchValue is not null)

			Begin
				set @totalResults=(select count(a.id) from (select im.id from InterviewMeetings im 
						inner join [dbo].[InterviewComments] ic on im.id = ic.InterviewId      
						inner join UserList ulc on ulc.EmailId = im.candidateEmail      
						inner join CandidatesMaster cm on ulc.id = cm.UserId      
						inner join QCFeedbackReport QCF on im.id=QCF.InterviewId      
						inner join IRF on im.IRFId = IRF.IRFId          
						inner join [dbo].[TransClientUser] tc on tc.IRFId = IRF.id      
						inner join dbo.UserList ul on ul.id = tc.ClientUserId      
						inner join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id  
						--inner join #tblTechSkill tts on tts.InterviewId = im.id
						left join @tblPatchCount tpc on tpc.InterviewId=ic.InterviewId
						where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and ul.EmailId=@Emailid  and (ic.MoveToCP is null or ic.MoveToCP =0) and (im.candidateName like ('%' +@searchValue +'%') or irf.JDTiltle like ('%' +@searchValue +'%'))
						group by im.id,im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,      
						im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,      
						im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,      
						im.currentEmployerName, im.QCInterviewer, im.IsEmergency,  im.JoiningBiometryScore,       
						ca.Phone, ca.ResumeLink,IRF.id,IRF.JDLink,IRF.JDTiltle,IRF.ClientId,
						IRF.ClientName,ic.AadharFileLink,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,
						ic.MeetingStatus, cm.id,ic.ThumbnailImage  , QCF.InterviewId ,ic.CandidateAvailable,
						tpc.MeetingPatchCount, ic.ThumbnailTimeSeconds, ic.TotalTechScore) a)

			End
		Else
			Begin
				set @totalResults=(select count(a.id) from (select im.id from InterviewMeetings im 
						inner join [dbo].[InterviewComments] ic on im.id = ic.InterviewId      
						inner join UserList ulc on ulc.EmailId = im.candidateEmail      
						inner join CandidatesMaster cm on ulc.id = cm.UserId      
						inner join QCFeedbackReport QCF on im.id=QCF.InterviewId      
						inner join IRF on im.IRFId = IRF.IRFId          
						inner join [dbo].[TransClientUser] tc on tc.IRFId = IRF.id      
						inner join dbo.UserList ul on ul.id = tc.ClientUserId      
						inner join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id 
						--inner join #tblTechSkill tts on tts.InterviewId = im.id
						left join @tblPatchCount tpc on tpc.InterviewId=ic.InterviewId
						where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and ul.EmailId=@Emailid and (ic.MoveToCP is null or ic.MoveToCP =0) 
						group by im.id,im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,      
						im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,      
						im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,      
						im.currentEmployerName, im.QCInterviewer, im.IsEmergency,  im.JoiningBiometryScore,       
						ca.Phone, ca.ResumeLink,IRF.id,IRF.JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,
						ic.AadharFileLink,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,
						ic.MeetingStatus, cm.id,ic.ThumbnailImage  , QCF.InterviewId ,ic.CandidateAvailable,
						tpc.MeetingPatchCount, ic.ThumbnailTimeSeconds, ic.TotalTechScore)a )

			End

	End



	select @totalResults
End

GO


