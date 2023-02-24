
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetRiseTubeIndexMeetingsCountForCuratedUserFilteredDup'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRiseTubeIndexMeetingsCountForCuratedUserFilteredDup
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetRiseTubeIndexMeetingsCountForCuratedUserFilteredDup]  
  @EmailidParam varchar(100),    
  @searchValueParam varchar(max) = null,
  @startDateParam datetime,
  @endDateParam datetime
AS  
Begin 
	declare @searchValue varchar(max), @Emailid varchar(100),
			@startDate datetime, @endDate datetime

		set @searchValue=@searchValueParam
		set @Emailid=@EmailIdParam
		set @startDate=@startDateParam
		set @endDate=@endDateParam

DECLARE @id int 

		Select @id=id from UserList where EmailId=@Emailid

	CREATE TABLE #PatchVideoCountTbl (InterviewId int, VideoCount int)

	insert into #PatchVideoCountTbl
	select vqd.Interviewid, count(vqd.id) from VideoQuestionDetails vqd
	join InterviewComments ic on ic.InterviewId = vqd.Interviewid
	where NULLIF(vqd.VideoLink, '') IS NOT NULL
	group by vqd.Interviewid

	CREATE TABLE #TechSkillScoreTbl (InterviewId int, WeightageActual float)

	insert into #TechSkillScoreTbl
	select tsr.Interviewid, sum(tsr.WeightageActual) from TechSkillReport tsr
	join InterviewComments ic on ic.InterviewId = tsr.Interviewid
	group by tsr.Interviewid

	CREATE TABLE #SuitableCandidateTbl (InterviewId int, [status] varchar(20))

	insert into #SuitableCandidateTbl
	Select top 1 suc.Interviewid, suc.[Status] from SuitableCandidate suc
	join InterviewComments ic on ic.InterviewId = suc.Interviewid
	where Userid=@id order by InterstedOn desc


	CREATE TABLE #RiseTubeVedioShortListTbl (InterviewId int, [status] varchar(20))

	insert into #RiseTubeVedioShortListTbl
	Select top 1 rvs.Interviewid, rvs.[Status] from RiseTubeVedioShortList rvs
	join InterviewComments ic on ic.InterviewId = rvs.Interviewid
	where Userid=@id order by ShortListedOn desc



if(@searchValue is not null)

	Begin

		select count(a.id) from (select im.id
				from InterviewMeetings im            
				inner join IRF on im.IRFId = IRF.IRFId          
				left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId       
				left join UserList ul on ul.EmailId = im.candidateEmail    
				join CandidatesMaster cm on cm.UserId = ul.id        
				join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id    
				--join CandidateCuratedScore ccs on ul.EmailId= ccs.CandidateEmailId    
				left join QCFeedbackReport QCF on im.id=QCF.InterviewId        
				inner join client cl on IRF.ClientID=cl.id        
				inner join CuratedIRFMapping cim on cim.IRFId = irf.id    
				inner join CuratedGroupSubscribers cgs on cgs.CuratedGroupId = cim.CuratedGroupId 
				left join #SuitableCandidateTbl as suit on suit.InterviewId = ic.InterviewId
				left join #RiseTubeVedioShortListTbl as rtvsl on rtvsl.InterviewId = ic.InterviewId
				left join #PatchVideoCountTbl as patchvideoCount on patchvideoCount.InterviewId = ic.InterviewId
				left join #TechSkillScoreTbl as Techscore on Techscore.InterviewId = ic.InterviewId
				where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and cgs.UserId=@id -- and ccs.CuratedEmailId=@Emailid and ccs.Score>=30 
				and (im.candidateName like ('%' +@searchValue +'%') or irf.JDTiltle like ('%' +@searchValue +'%')) 
				
				and cast(interviewDate as date) >= cast (@startDate as date)
				and cast(interviewDate as date) <= cast (@endDate as date) and ISNULL(im.RedoInterview,0)=0

				group by im.id, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,    
				im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,
				im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,    
				im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.JoiningBiometryScore,     
				ca.Phone, ca.ResumeLink ,  ca.CostEstimate,IRF.id ,IRF.JDLink ,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,ic.VideoLink,    
				ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id ,ic.ThumbnailImage       
				, QCF.InterviewId ,suit.Status ,rtvsl.Status ,ic.CandidateAvailable,patchvideoCount.VideoCount,Techscore.WeightageActual,
				ic.ThumbnailTimeSeconds) a

	End

Else
	Begin
		select count(a.id) from (select im.id
				from InterviewMeetings im            
				inner join IRF on im.IRFId = IRF.IRFId          
				left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId       
				left join UserList ul on ul.EmailId = im.candidateEmail    
				join CandidatesMaster cm on cm.UserId = ul.id        
				join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id    
				--join CandidateCuratedScore ccs on ul.EmailId= ccs.CandidateEmailId    
				left join QCFeedbackReport QCF on im.id=QCF.InterviewId        
				inner join client cl on IRF.ClientID=cl.id        
				inner join CuratedIRFMapping cim on cim.IRFId = irf.id    
				inner join CuratedGroupSubscribers cgs on cgs.CuratedGroupId = cim.CuratedGroupId 
				left join #SuitableCandidateTbl as suit on suit.InterviewId = ic.InterviewId
				left join #RiseTubeVedioShortListTbl as rtvsl on rtvsl.InterviewId = ic.InterviewId
				left join #PatchVideoCountTbl as patchvideoCount on patchvideoCount.InterviewId = ic.InterviewId
				left join #TechSkillScoreTbl as Techscore on Techscore.InterviewId = ic.InterviewId
				where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and cgs.UserId=@id  --and ccs.CuratedEmailId=@Emailid and ccs.Score>=30
				
				and cast(interviewDate as date) >= cast (@startDate as date)
				and cast(interviewDate as date) <= cast (@endDate as date) and ISNULL(im.RedoInterview,0)=0

				group by im.id, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,    
				im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,
				im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,    
				im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.JoiningBiometryScore,     
				ca.Phone, ca.ResumeLink ,  ca.CostEstimate,IRF.id ,IRF.JDLink ,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,ic.VideoLink,    
				ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id ,ic.ThumbnailImage       
				, QCF.InterviewId ,suit.Status ,rtvsl.Status ,ic.CandidateAvailable,patchvideoCount.VideoCount,
				Techscore.WeightageActual, ic.ThumbnailTimeSeconds) a
	End

	drop table #PatchVideoCountTbl
	drop table #RiseTubeVedioShortListTbl
	drop table #SuitableCandidateTbl
	drop table #TechSkillScoreTbl

End

GO


