/****** Object:  StoredProcedure [dbo].[usp_GetSharedRiseTubeMeetingsForCuratedUserPaged]    Script Date: 02-11-2020 2.20.11 PM ******/


IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetSharedRiseTubeMeetingsForClientPaged'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSharedRiseTubeMeetingsForClientPaged
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create PROCEDURE [dbo].[usp_GetSharedRiseTubeMeetingsForClientPaged] 
	@startFromParam int,
	@endWithParam int,
	@searchValueParam varchar(max) = null
AS            
Begin  

declare @startFrom INT, @endWith int, @searchValue varchar(max)
set @startFrom = @startFromParam
set @endWith = @endWithParam
set @searchValue=@searchValueParam

--declare @tblTechSkill table (InterviewId int, TechSkillScore float);
--	insert into @tblTechSkill
--	select InterviewId, sum(WeightageActual) as TechSkillScore from TechSkillReport group by InterviewId

CREATE TABLE #tblTechSkill (InterviewId int, TechSkillScore float)

insert into #tblTechSkill
select tsr.Interviewid, sum(tsr.WeightageActual) from TechSkillReport tsr
group by tsr.Interviewid

CREATE TABLE #PatchVideoCountTbl (InterviewId int, VideoCount int)

	insert into #PatchVideoCountTbl
	select vqd.Interviewid, count(vqd.id) from VideoQuestionDetails vqd
	join InterviewComments ic on ic.InterviewId = vqd.Interviewid
	where NULLIF(vqd.VideoLink, '') IS NOT NULL
	group by vqd.Interviewid

if(@searchValue is not null)

	Begin
		;WITH meetings_rows AS (select ROW_NUMBER() OVER (
		ORDER BY im.interviewDate desc
		) row_num, im.id as imid, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,        
		im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,    
		im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,        
		im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.JoiningBiometryScore,         
		ca.Phone as MobileNumber, ca.EncodedResumeLink as resumeLink, ca.CostEstimate,       
		IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,ic.VideoLink,        
		ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id as CandidateId,ic.ThumbnailImage           
		as ThumbnailImage,    
		patchvideoCount.VideoCount as MeetingPatchCount,    
		(CASE WHEN ic.CandidateAvailable = 1 THEN 'Available' ELSE 'NotAvailable' END)  as CandidateAvailable,      
		 tsr.TechSkillScore,ca.TotalExperience, ca.Heuristicsscore, hl.[Description] as HeuristicDesc      
		from InterviewMeetings im                
		inner join IRF on im.IRFId = IRF.IRFId         
		inner join CuratedIRFMapping cim on cim.IRFId = irf.id     
		inner join [dbo].[InterviewComments] ic on im.id = ic.InterviewId           
		inner join UserList ul on ul.EmailId = im.candidateEmail        
		join CandidatesMaster cm on cm.UserId = ul.id            
		join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id    
		inner join #tblTechSkill tsr  on tsr.InterviewId= im.id
		left join HeuristicLevels hl on hl.id = ca.Heuristicsscore
		left join #PatchVideoCountTbl as patchvideoCount on patchvideoCount.InterviewId = ic.InterviewId
		--outer apply (select count(*) as MeetingCount from VideoQuestionDetails where Interviewid=ic.InterviewId and VideoLink!='')patchvideoCount    
    
		where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and ic.CandidateAvailable=1 and (im.candidateName like ('%' +@searchValue +'%') or irf.JDTiltle like ('%' +@searchValue +'%')) and ca.AvailableFullTime=1 and ca.AvailableFreeLancing=1     
		group by im.id, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,        
		im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,    
		im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,        
		im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.JoiningBiometryScore,         
		ca.Phone, ca.EncodedResumeLink ,  ca.CostEstimate,tsr.TechSkillScore,    
		IRF.id ,IRF.JDLink ,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,ic.VideoLink,        
		ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id ,ic.ThumbnailImage,patchvideoCount.VideoCount           
		 ,ic.CandidateAvailable,ca.TotalExperience, ca.Heuristicsscore, hl.[Description])
		 select * from meetings_rows where row_num>@startFrom and row_num<=@endWith 

	End

Else
	Begin
		;WITH meetings_rows AS (select ROW_NUMBER() OVER (
		ORDER BY im.interviewDate desc
		) row_num, im.id as imid, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,        
		im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,    
		im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,        
		im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.JoiningBiometryScore,         
		ca.Phone as MobileNumber, ca.EncodedResumeLink as resumeLink, ca.CostEstimate,       
		IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,ic.VideoLink,        
		ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id as CandidateId,ic.ThumbnailImage           
		as ThumbnailImage,    
		patchvideoCount.VideoCount as MeetingPatchCount,    
		(CASE WHEN ic.CandidateAvailable = 1 THEN 'Available' ELSE 'NotAvailable' END)  as CandidateAvailable,      
		 tsr.TechSkillScore,ca.TotalExperience, ca.Heuristicsscore, hl.[Description] as HeuristicDesc     
		from InterviewMeetings im                
		inner join IRF on im.IRFId = IRF.IRFId         
		inner join CuratedIRFMapping cim on cim.IRFId = irf.id     
		inner join [dbo].[InterviewComments] ic on im.id = ic.InterviewId           
		inner join UserList ul on ul.EmailId = im.candidateEmail        
		join CandidatesMaster cm on cm.UserId = ul.id            
		join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id    
		inner join #tblTechSkill tsr  on tsr.InterviewId= im.id
		left join HeuristicLevels hl on hl.id = ca.Heuristicsscore
		left join #PatchVideoCountTbl as patchvideoCount on patchvideoCount.InterviewId = ic.InterviewId
		--outer apply (select count(*) as MeetingCount from VideoQuestionDetails where Interviewid=ic.InterviewId and VideoLink!='')patchvideoCount    
    
		where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and ic.CandidateAvailable=1 and ca.AvailableFullTime=1 and ca.AvailableFreeLancing=1     
		group by im.id, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, im.candidateEmail, im.ClientHrContact,        
		im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,    
		im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, im.candidateName,        
		im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.JoiningBiometryScore,         
		ca.Phone, ca.EncodedResumeLink ,  ca.CostEstimate,tsr.TechSkillScore,    
		IRF.id ,IRF.JDLink ,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,ic.VideoLink,        
		ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id ,ic.ThumbnailImage,patchvideoCount.VideoCount           
		 ,ic.CandidateAvailable,ca.TotalExperience, ca.Heuristicsscore, hl.[Description])
		 select * from meetings_rows where row_num>@startFrom and row_num<=@endWith
	End

drop table #PatchVideoCountTbl
drop table #tblTechSkill
  
END      
   
GO


