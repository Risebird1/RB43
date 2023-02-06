    
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetRiseTubeMeetingsByIRFIdForCuratedUser'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRiseTubeMeetingsByIRFIdForCuratedUser
END


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO    
    
CREATE PROCEDURE [dbo].[usp_GetRiseTubeMeetingsByIRFIdForCuratedUser]        
  @Emailid varchar(100),    
  @IRFId varchar(100),    
  @startDate datetime,    
  @endDate datetime    
AS        
Begin       
    
DECLARE     
 @id int,    
 @EmailIdParam varchar(100),     
 @IRFIdParam varchar(100),    
 @startDateParam datetime,    
 @endDateParam datetime     
     
 set @EmailIdParam=@Emailid    
 set @IRFIdParam =@IRFId    
 set @startDateParam =@startDate    
 set @endDateParam=@endDate    
    
 Select @id=id from UserList where EmailId=@EmailIdParam      

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

	
     
    
select distinct im.id as imid, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, ul.EmailId as candidateEmail, im.ClientHrContact,        
im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,    
im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy,   
case   
  when ul.LastName is null then ul.FirstName  
  else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))  
end as candidateName,         
im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,         
ca.Phone as MobileNumber, ca.ResumeLink as resumeLink,  ca.CostEstimate,     
IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,ic.VideoLink,        
ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id as CandidateId,ic.ThumbnailImage           
as ThumbnailImage, QCF.InterviewId as QCInterview,    
suit.Status as IsIntrested ,    
rtvsl.Status as ShortListstatus,    
patchvideoCount.VideoCount as MeetingPatchCount,    
(CASE WHEN ic.CandidateAvailable = 1 THEN 'Available' ELSE 'NotAvailable' END)  as CandidateAvailable,      
 ic.TotalTechScore as TechSkillScore, ic.ThumbnailTimeSeconds      
from InterviewMeetings im                
inner join IRF on im.IRFId = IRF.IRFId              
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId           
left join UserList ul on ul.EmailId = im.candidateEmail        
join CandidatesMaster cm on cm.UserId = ul.id            
join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id        
join CandidateCuratedScore ccs on ul.EmailId= ccs.CandidateEmailId        
left join QCFeedbackReport QCF on im.id=QCF.InterviewId            
inner join client cl on IRF.ClientID=cl.id            
inner join CuratedIRFMapping cim on cim.IRFId = irf.id        
inner join CuratedGroupSubscribers cgs on cgs.CuratedGroupId = cim.CuratedGroupId        
--inner join TechSkillReport tsr  on tsr.InterviewId= im.id 
left join #SuitableCandidateTbl as suit on suit.InterviewId = ic.InterviewId
left join #RiseTubeVedioShortListTbl as rtvsl on rtvsl.InterviewId = ic.InterviewId
left join #PatchVideoCountTbl as patchvideoCount on patchvideoCount.InterviewId = ic.InterviewId
--left join #TechSkillScoreTbl as Techscore on Techscore.InterviewId = ic.InterviewId
--outer apply (Select top 1 status from SuitableCandidate where InterviewId= ic.InterviewId and Userid=@id order by InterstedOn desc)suit    
--outer apply (Select top 1 status from RiseTubeVedioShortList where InterviewId= ic.InterviewId and Userid=@id  order by ShortListedOn desc)rtvsl    
--outer apply (select count(*) as MeetingCount from VideoQuestionDetails where Interviewid=ic.InterviewId and NULLIF(VideoLink, '') IS NOT NULL)patchvideoCount    
--outer apply (select sum(WeightageActual) as TechSkillScore from TechSkillReport where Interviewid=ic.InterviewId )Techscore    
where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and cgs.UserId=@id  and ccs.CuratedEmailId=@EmailIdParam and ccs.Score>=30 and IRF.IRFId = @IRFIdParam--IRF.JDTiltle LIKE ('%' +@JDPatternParam+'%')    
and cast(im.interviewDate as date)>=cast(@startDateParam as date) and cast(im.interviewDate as date)<=cast(@endDateParam as date)    
group by im.id, im.IRFId, im.interviewDate, im.interviewSlot, im.interviewDuration, ul.EmailId, im.ClientHrContact,        
im.confirmationStatus, im.zoomLink, im.risebirdHR, im.interviewer, im.IntervIewOnDevice, im.PreferredLocation,IRF.IsBiometricEnabled,  im.InterviewBiometryScore,    
im.ExpectedPerHourRate, im.staffingAgency, im.joiningTime, im.sCreatedBy, im.dtCreatedOn, im.sModifiedBy, ul.FirstName, ul.LastName,        
im.currentEmployerName, im.QCInterviewer, im.IsEmergency, im.InterviewBiometryScore, im.JoiningBiometryScore,         
ca.Phone, ca.ResumeLink ,  ca.CostEstimate,IRF.id ,IRF.JDLink ,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,ic.AadharFileLink,ic.VideoLink,        
ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id ,ic.ThumbnailImage           
, QCF.InterviewId, suit.Status, rtvsl.Status , ic.CandidateAvailable, patchvideoCount.VideoCount, ic.TotalTechScore,
ic.ThumbnailTimeSeconds  


drop table #PatchVideoCountTbl
drop table #RiseTubeVedioShortListTbl
drop table #SuitableCandidateTbl
--drop table #TechSkillScoreTbl

END 

Go