IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetIRFMutliStackInterviewStatusDetailedView'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFMutliStackInterviewStatusDetailedView
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_GetIRFMutliStackInterviewStatusDetailedView    
@irfname varchar(15),    
@type varchar(50),  
@startdate datetime,  
@timenow datetime  
AS    
BEGIN    
DECLARE @TemporaryCandidateTableVariable TABLE (                        
 IRFId int,                  
 CandidateMasterId int,                  
 UserId int,             
 CandidateLatestStatus varchar(20)                  
);     
  
INSERT INTO @TemporaryCandidateTableVariable                        
select ca.IRFId, ca.CandidateMasterId, cm.UserId,                    
dbo.fn_CandidateLatestStatusForIrfMultiStackStats(cm.UserId, ca.IRFId, @startdate, @timenow)                  
from  Candidate ca                   
join CandidateStatus cs on  (ca.IRFId = cs.IRFId and ca.CandidateMasterId = cs.CandidateId)                   
join CandidatesMaster cm on cm.id=ca.CandidateMasterId      
join IRF tt on tt.id=ca.IRFId   
where tt.IRFId=@irfname  
group by ca.IRFId, ca.CandidateMasterId,cm.UserId      
  
if(@type='Completed')    
begin    
 ;with CTE as(Select tt.CandidateMasterId,tt.UserId,tt.CandidateLatestStatus,MAx(im.interviewdate) interviewdate,im.candidateEmail,im.IRFId From @TemporaryCandidateTableVariable tt join IRF on IRF.id=tt.IRFId
join UserList ul on ul.id=tt.UserId  
join InterviewMeetings im on im.IRFId=IRF.IRFId and im.candidateEmail=ul.EmailId  
where ISNULL(im.isdeleted,0)=0
group by tt.CandidateMasterId,tt.UserId,tt.CandidateLatestStatus,im.candidateEmail,im.IRFId)
Select distinct cast(im.confirmationStatus as int) confirmationStatus, ic.MeetingStatus, ic.InterviewStatus,   
im.interviewDate,ice.InterviewStatus as InterviewStatusTwo,  
im.candidateEmail,im.candidateName,IRF.JDTiltle,IRF.JDTitleTwo,ip.NickName as InterviewerCodedName,techone.TechScoreone,techtwo.TechScoretwo,im.ClientHrContact,im.id,IRF.id as Irfintid,im.CodeSnippet,im.CodeSnippetTwo  
from CTE join interviewMeetings im on im.Interviewdate=CTE.interviewdate and im.candidateEmail=CTE.candidateEmail and im.IRFId=CTE.IRFId
join IRF on IRF.IRFId=im.IRFId
join InterviewComments ic on ic.Interviewid=im.id  
left join InterviewCommentsExtension ice on ice.Interviewid=im.id 
left join UserList uli on uli.EmailId=im.interviewer  
left join InterviewerProfile ip on ip.userid=uli.id  
outer Apply (Select Sum(WeightageActual) as TechScoreone from TechSkillReport where skillset!=2 and Interviewid=im.id) techone  
outer Apply (Select Sum(WeightageActual) as TechScoretwo from TechSkillReport where skillset=2 and Interviewid=im.id) techtwo  
where CTE.CandidateLatestStatus in ('BOTHSELECTED', 'BOTHREJECTED','SELECTEDNREJECTED','REJECTEDNSELECTED') 
order by im.interviewdate desc  
end    
else if(@type='Selected')  
begin  
 ;with CTE as(Select tt.CandidateMasterId,tt.UserId,tt.CandidateLatestStatus,MAx(im.interviewdate) interviewdate,im.candidateEmail,im.IRFId From @TemporaryCandidateTableVariable tt join IRF on IRF.id=tt.IRFId
join UserList ul on ul.id=tt.UserId  
join InterviewMeetings im on im.IRFId=IRF.IRFId and im.candidateEmail=ul.EmailId  
where ISNULL(im.isdeleted,0)=0
group by tt.CandidateMasterId,tt.UserId,tt.CandidateLatestStatus,im.candidateEmail,im.IRFId)
Select distinct cast(im.confirmationStatus as int) confirmationStatus, ic.MeetingStatus, ic.InterviewStatus,   
im.interviewDate,ice.InterviewStatus as InterviewStatusTwo,  
im.candidateEmail,im.candidateName,IRF.JDTiltle,IRF.JDTitleTwo,ip.NickName as InterviewerCodedName,techone.TechScoreone,techtwo.TechScoretwo,im.ClientHrContact,im.id,IRF.id as Irfintid,im.CodeSnippet,im.CodeSnippetTwo  
from CTE join interviewMeetings im on im.Interviewdate=CTE.interviewdate and im.candidateEmail=CTE.candidateEmail and im.IRFId=CTE.IRFId
join IRF on IRF.IRFId=im.IRFId
join InterviewComments ic on ic.Interviewid=im.id  
left join InterviewCommentsExtension ice on ice.Interviewid=im.id 
left join UserList uli on uli.EmailId=im.interviewer  
left join InterviewerProfile ip on ip.userid=uli.id  
outer Apply (Select Sum(WeightageActual) as TechScoreone from TechSkillReport where skillset!=2 and Interviewid=im.id) techone  
outer Apply (Select Sum(WeightageActual) as TechScoretwo from TechSkillReport where skillset=2 and Interviewid=im.id) techtwo  
where CTE.CandidateLatestStatus in ('BOTHSELECTED') 
order by im.interviewdate desc  
end  
else if(@type='Rejected')  
begin  
;with CTE as(Select tt.CandidateMasterId,tt.UserId,tt.CandidateLatestStatus,MAx(im.interviewdate) interviewdate,im.candidateEmail,im.IRFId From @TemporaryCandidateTableVariable tt join IRF on IRF.id=tt.IRFId
join UserList ul on ul.id=tt.UserId  
join InterviewMeetings im on im.IRFId=IRF.IRFId and im.candidateEmail=ul.EmailId  
where ISNULL(im.isdeleted,0)=0
group by tt.CandidateMasterId,tt.UserId,tt.CandidateLatestStatus,im.candidateEmail,im.IRFId)
Select distinct cast(im.confirmationStatus as int) confirmationStatus, ic.MeetingStatus, ic.InterviewStatus,   
im.interviewDate,ice.InterviewStatus as InterviewStatusTwo,  
im.candidateEmail,im.candidateName,IRF.JDTiltle,IRF.JDTitleTwo,ip.NickName as InterviewerCodedName,techone.TechScoreone,techtwo.TechScoretwo,im.ClientHrContact,im.id,IRF.id as Irfintid,im.CodeSnippet,im.CodeSnippetTwo  
from CTE join interviewMeetings im on im.Interviewdate=CTE.interviewdate and im.candidateEmail=CTE.candidateEmail and im.IRFId=CTE.IRFId
join IRF on IRF.IRFId=im.IRFId
join InterviewComments ic on ic.Interviewid=im.id  
left join InterviewCommentsExtension ice on ice.Interviewid=im.id 
left join UserList uli on uli.EmailId=im.interviewer  
left join InterviewerProfile ip on ip.userid=uli.id  
outer Apply (Select Sum(WeightageActual) as TechScoreone from TechSkillReport where skillset!=2 and Interviewid=im.id) techone  
outer Apply (Select Sum(WeightageActual) as TechScoretwo from TechSkillReport where skillset=2 and Interviewid=im.id) techtwo  
where CTE.CandidateLatestStatus in ('BOTHREJECTED') 
order by im.interviewdate desc  
end  
else if(@type='SelectedRejected')  
begin  
 ;with CTE as(Select tt.CandidateMasterId,tt.UserId,tt.CandidateLatestStatus,MAx(im.interviewdate) interviewdate,im.candidateEmail,im.IRFId From @TemporaryCandidateTableVariable tt join IRF on IRF.id=tt.IRFId
join UserList ul on ul.id=tt.UserId  
join InterviewMeetings im on im.IRFId=IRF.IRFId and im.candidateEmail=ul.EmailId  
where ISNULL(im.isdeleted,0)=0
group by tt.CandidateMasterId,tt.UserId,tt.CandidateLatestStatus,im.candidateEmail,im.IRFId)
Select distinct cast(im.confirmationStatus as int) confirmationStatus, ic.MeetingStatus, ic.InterviewStatus,   
im.interviewDate,ice.InterviewStatus as InterviewStatusTwo,  
im.candidateEmail,im.candidateName,IRF.JDTiltle,IRF.JDTitleTwo,ip.NickName as InterviewerCodedName,techone.TechScoreone,techtwo.TechScoretwo,im.ClientHrContact,im.id,IRF.id as Irfintid,im.CodeSnippet,im.CodeSnippetTwo  
from CTE join interviewMeetings im on im.Interviewdate=CTE.interviewdate and im.candidateEmail=CTE.candidateEmail and im.IRFId=CTE.IRFId
join IRF on IRF.IRFId=im.IRFId
join InterviewComments ic on ic.Interviewid=im.id  
left join InterviewCommentsExtension ice on ice.Interviewid=im.id 
left join UserList uli on uli.EmailId=im.interviewer  
left join InterviewerProfile ip on ip.userid=uli.id  
outer Apply (Select Sum(WeightageActual) as TechScoreone from TechSkillReport where skillset!=2 and Interviewid=im.id) techone  
outer Apply (Select Sum(WeightageActual) as TechScoretwo from TechSkillReport where skillset=2 and Interviewid=im.id) techtwo  
where CTE.CandidateLatestStatus in ('SELECTEDNREJECTED') 
order by im.interviewdate desc  
end  
else if(@type='RejectedSelected')  
begin  
 ;with CTE as(Select tt.CandidateMasterId,tt.UserId,tt.CandidateLatestStatus,MAx(im.interviewdate) interviewdate,im.candidateEmail,im.IRFId From @TemporaryCandidateTableVariable tt join IRF on IRF.id=tt.IRFId
join UserList ul on ul.id=tt.UserId  
join InterviewMeetings im on im.IRFId=IRF.IRFId and im.candidateEmail=ul.EmailId  
where ISNULL(im.isdeleted,0)=0
group by tt.CandidateMasterId,tt.UserId,tt.CandidateLatestStatus,im.candidateEmail,im.IRFId)
Select distinct cast(im.confirmationStatus as int) confirmationStatus, ic.MeetingStatus, ic.InterviewStatus,   
im.interviewDate,ice.InterviewStatus as InterviewStatusTwo,  
im.candidateEmail,im.candidateName,IRF.JDTiltle,IRF.JDTitleTwo,ip.NickName as InterviewerCodedName,techone.TechScoreone,techtwo.TechScoretwo,im.ClientHrContact,im.id,IRF.id as Irfintid,im.CodeSnippet,im.CodeSnippetTwo  
from CTE join interviewMeetings im on im.Interviewdate=CTE.interviewdate and im.candidateEmail=CTE.candidateEmail and im.IRFId=CTE.IRFId
join IRF on IRF.IRFId=im.IRFId
join InterviewComments ic on ic.Interviewid=im.id  
left join InterviewCommentsExtension ice on ice.Interviewid=im.id 
left join UserList uli on uli.EmailId=im.interviewer  
left join InterviewerProfile ip on ip.userid=uli.id  
outer Apply (Select Sum(WeightageActual) as TechScoreone from TechSkillReport where skillset!=2 and Interviewid=im.id) techone  
outer Apply (Select Sum(WeightageActual) as TechScoretwo from TechSkillReport where skillset=2 and Interviewid=im.id) techtwo  
where CTE.CandidateLatestStatus in ('REJECTEDNSELECTED') 
order by im.interviewdate desc  
end  
else if(@type='ColdCandidate')
begin
Select RTRIM(LTRIM(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,''))) as candidateName,ul.emailid as candidateEmail,ul.MobileNumber,LatestStatus.StatusValue,ca.ClientHRAttemptContact as ClientHrContact
from @TemporaryCandidateTableVariable tt join UserList ul on ul.id=tt.Userid
join Candidate ca on ca.CandidateMasterId=tt.CandidateMasterId and ca.IRFId=tt.IRFId
outer Apply (Select top 1 csm.StatusValue from CandidateStatus cs join CandidateStatusOptionsMst csm on csm.id=cs.Status  where cs.candidateid=tt.CandidateMasterId and cs.IRFId=tt.IRFId order by Date desc) LatestStatus
where CandidateLatestStatus='COLDCANDIDATE'
end
else if(@type='NotScheduled')
begin
Select RTRIM(LTRIM(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,''))) as candidateName,ul.emailid as candidateEmail,ul.MobileNumber,LatestStatus.StatusValue,ca.ClientHRAttemptContact as ClientHrContact
from @TemporaryCandidateTableVariable tt join UserList ul on ul.id=tt.Userid
join Candidate ca on ca.CandidateMasterId=tt.CandidateMasterId and ca.IRFId=tt.IRFId
outer Apply (Select top 1 csm.StatusValue from CandidateStatus cs join CandidateStatusOptionsMst csm on csm.id=cs.Status  where cs.candidateid=tt.CandidateMasterId and cs.IRFId=tt.IRFId order by Date desc) LatestStatus
where CandidateLatestStatus='NOTSCHEDULED'
end
else if(@type='Reschedule')
begin
Select 'RESCHEDULE'
end
END 
 GO