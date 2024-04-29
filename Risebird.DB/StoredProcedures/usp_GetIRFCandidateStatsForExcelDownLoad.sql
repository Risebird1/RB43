
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIRFCandidateStatsForExcelDownLoad'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFCandidateStatsForExcelDownLoad
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_GetIRFCandidateStatsForExcelDownLoad  
@irfId int,         
@timeNow datetime  
AS  
BEGIN  
             
--INSERT INTO @tbl_Temp              
--select distinct IRFId from CandidateStatus where [Status] =12 and IRFId = @irfId
DECLARE @TemporaryCandidateTableVariable TABLE (              
 IRFId int,        
 CandidateMasterId int,        
 UserId int,   
 CandidateLatestStatus varchar(20),
 InterviewId int null,
 interviewDate datetime null,
 ResumeLink varchar(500) NULL--,
 --AddedOnDate datetime null,
 --InvitationSentOnDate datetime null,
 --FirstScheduledOnDate datetime null,
 --FirstScheduledOninterviewDate datetime null
);           
        --@AddedOnDate datetime, @InvitationSentOnDate datetime, @FirstScheduledOnDate datetime,  @FirstScheduledOninterviewDate
INSERT INTO @TemporaryCandidateTableVariable              
select ca.IRFId, ca.CandidateMasterId, cm.UserId, La.LatestStatus, La.interviewId, La.interviewDate,  ca.ResumeLink
--,Journey.AddedOnDate, Journey.InvitationSentOnDate, Journey.FirstScheduledOnDate, Journey.FirstScheduledOninterviewDate       
--dbo.fn_CandidateLatestStatusForIrfStats(cm.UserId, ca.IRFId, cast(DATEADD(year, -4, getdate()) as date), @timeNow)        
from  Candidate ca         
join CandidateStatus cs on  (ca.IRFId = cs.IRFId and ca.CandidateMasterId = cs.CandidateId)         
join CandidatesMaster cm on cm.id=ca.CandidateMasterId  
outer apply dbo.fn_CandidateLatestStatusForIrfStatsForExcel(cm.UserId, ca.IRFId, @timeNow) La
--outer apply dbo.fn_CandidateJourneyImportantMileStoneData(cm.UserId, ca.IRFId) Journey
where cs.[Status] = 12 and  ca.IRFId = @irfId-- cast([Date] as date) <=cast(@endDate as date) and cast([Date] as date) >= cast(@startDate as date)        
group by ca.IRFId, ca.CandidateMasterId,cm.UserId, La.LatestStatus, La.interviewId, La.interviewDate, ca.ResumeLink
--, Journey.AddedOnDate, Journey.InvitationSentOnDate, Journey.FirstScheduledOnDate, Journey.FirstScheduledOninterviewDate 


----completed

select im.id, im.IRFId, im.interviewDate, im.candidateEmail, im.candidateName, ul.MobileNumber, IRF.JDTiltle, ic.MeetingStatus,
ic.InterviewStatus, ic.InterviewerComment, im.ClientHrContact, ic.VideoLink, im.confirmationStatus, ic.CommunicationalProficiency,
im.staffingAgency, tctv.ResumeLink, CandidateLatestStatus--, tctv.AddedOnDate, tctv.InvitationSentOnDate, tctv.FirstScheduledOnDate, tctv.FirstScheduledOninterviewDate
, psd.isQcDone, irf.ClientId --177 for turing
from @TemporaryCandidateTableVariable tctv
join InterviewMeetings im on im.id = tctv.InterviewId
join IRF on IRF.IRFId=im.IRFId 
join InterviewComments ic on ic.InterviewId=im.id
join UserList ul on ul.EmailId = im.candidateEmail
left join PaymentSLADetails psd on psd.Interviewid = im.id
--join CandidatesMaster cm on cm.UserId = ul.id
--join Candidate ca on (ca.CandidateMasterId = cm.id and ca.IRFId = irf.id)
where CandidateLatestStatus in ('SELECTED', 'REJECTED')


--Profile Added Date, invitation sent date, scheduled date, complted date

select a.*, Skill.[Name] as SkillName from 
(select ts.*, [LevelTech].[Name] as RequiredLevel from 
TechSkillReport ts inner join [LevelTech] on ts.RequiredLevelId=[LevelTech].id) as a inner join Skill on a.SkillId = Skill.id 
join InterviewMeetings im on im.id=a.InterviewId
join @TemporaryCandidateTableVariable tctv on tctv.InterviewId = im.id
join IRF on IRF.IRFId=im.IRFId
where CandidateLatestStatus in ('SELECTED', 'REJECTED')


----pending

select im.id, im.IRFId, im.interviewDate, im.candidateEmail, im.candidateName, ul.MobileNumber, IRF.JDTiltle, ic.MeetingStatus,
ic.InterviewStatus, ic.InterviewerComment, im.ClientHrContact, ic.VideoLink, im.confirmationStatus, ic.CommunicationalProficiency,
im.staffingAgency, tctv.ResumeLink, CandidateLatestStatus--, tctv.AddedOnDate, tctv.InvitationSentOnDate, tctv.FirstScheduledOnDate, tctv.FirstScheduledOninterviewDate
from @TemporaryCandidateTableVariable tctv
join InterviewMeetings im on im.id = tctv.InterviewId
join IRF on IRF.IRFId=im.IRFId 
join InterviewComments ic on ic.InterviewId=im.id
join UserList ul on ul.EmailId = im.candidateEmail
--join CandidatesMaster cm on cm.UserId = ul.id
--join Candidate ca on (ca.CandidateMasterId = cm.id and ca.IRFId = irf.id)
where CandidateLatestStatus in ('PENDING')


----future scheduled
select im.id, im.IRFId, im.interviewDate, im.candidateEmail, im.candidateName, ul.MobileNumber, IRF.JDTiltle, im.confirmationStatus
, im.ClientHrContact, im.staffingAgency
--, ic.MeetingStatus,ic.InterviewStatus, ic.InterviewerComment, ic.VideoLink, ic.CommunicationalProficiency,
, tctv.ResumeLink, CandidateLatestStatus--, tctv.AddedOnDate, tctv.InvitationSentOnDate, tctv.FirstScheduledOnDate, tctv.FirstScheduledOninterviewDate
from @TemporaryCandidateTableVariable tctv
join InterviewMeetings im on im.id = tctv.InterviewId
join IRF on IRF.IRFId=im.IRFId 
--join InterviewComments ic on ic.InterviewId=im.id
join UserList ul on ul.EmailId = im.candidateEmail
--join CandidatesMaster cm on cm.UserId = ul.id
--join Candidate ca on (ca.CandidateMasterId = cm.id and ca.IRFId = irf.id)
where CandidateLatestStatus in ('FUTURESCHEDULE')

----rescheduled

select im.id, im.IRFId, im.interviewDate, im.candidateEmail, im.candidateName, ul.MobileNumber, IRF.JDTiltle, ic.MeetingStatus,
ic.InterviewStatus, im.ClientHrContact, ic.VideoLink, im.confirmationStatus--, ic.CommunicationalProficiency, ic.InterviewerComment
,im.staffingAgency, tctv.ResumeLink, CandidateLatestStatus--, tctv.AddedOnDate, tctv.InvitationSentOnDate, tctv.FirstScheduledOnDate, tctv.FirstScheduledOninterviewDate
from @TemporaryCandidateTableVariable tctv
join InterviewMeetings im on im.id = tctv.InterviewId
join IRF on IRF.IRFId=im.IRFId 
join InterviewComments ic on ic.InterviewId=im.id
join UserList ul on ul.EmailId = im.candidateEmail
--join CandidatesMaster cm on cm.UserId = ul.id
--join Candidate ca on (ca.CandidateMasterId = cm.id and ca.IRFId = irf.id)
where CandidateLatestStatus in ('RESCHEDULE')


----not scheduled
select ul.id as UserId, ul.MobileNumber, IRF.JDTiltle, ul.EmailId as candidateEmail, (ul.FirstName + ' ' + ul.LastName) as candidateName, irf.IRFId  --ic.MeetingStatus,--im.id, im.IRFId, im.interviewDate, im.candidateEmail, im.candidateName, 
--ic.InterviewStatus, ic.InterviewerComment,  ic.VideoLink, ic.CommunicationalProficiency,--im.ClientHrContact, im.confirmationStatus, im.staffingAgency, 
,tctv.ResumeLink, CandidateLatestStatus--, tctv.AddedOnDate, tctv.InvitationSentOnDate, tctv.FirstScheduledOnDate, tctv.FirstScheduledOninterviewDate
from @TemporaryCandidateTableVariable tctv
--left join InterviewMeetings im on im.id = tctv.InterviewId
join IRF on IRF.id =tctv.IRFId 
--join InterviewComments ic on ic.InterviewId=im.id
join UserList ul on ul.id = tctv.UserId
--join CandidatesMaster cm on cm.UserId = ul.id
--join Candidate ca on (ca.CandidateMasterId = cm.id and ca.IRFId = irf.id)
where CandidateLatestStatus in ('NOTSCHEDULED')

END
GO

