IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetPartnerDetailsStatsDetailedView'))
BEGIN
    DROP PROCEDURE dbo.usp_GetPartnerDetailsStatsDetailedView
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_GetPartnerDetailsStatsDetailedView  
@userid int,  
@type varchar(50),
@startDate datetime,                
@endDate datetime,                        
@timeNow datetime    
AS      
BEGIN      
DECLARE @TemporaryCandidateTableVariable TABLE (                          
 IRFId int,                    
 CandidateMasterId int,                    
 UserId int,               
 CandidateLatestStatus varchar(20),
 ClientHRAttemptContact varchar(200)
);            
Declare @ClientHRAttemptContact varchar(200)
Select @ClientHRAttemptContact=EmailId From UserList where id=@userid
    
INSERT INTO @TemporaryCandidateTableVariable                        
select ca.IRFId, ca.CandidateMasterId, cm.UserId,                    
dbo.fn_CandidateLatestStatusForIrfStats(cm.UserId, ca.IRFId, cast(@startDate as date), @timeNow),ca.ClientHRAttemptContact
from  Candidate ca                   
join CandidateStatus cs on  (ca.IRFId = cs.IRFId and ca.CandidateMasterId = cs.CandidateId)                   
join CandidatesMaster cm on cm.id=ca.CandidateMasterId  
join IRF on IRF.id=ca.IRFId
Where ca.ClientHRAttemptContact=@ClientHRAttemptContact  and cs.[Status] =12 and cast(cs.[Date] as date) <= cast(@endDate as date) and cast(cs.[Date] as date) >= cast(@startDate as date)
group by ca.IRFId, ca.CandidateMasterId,cm.UserId,ca.ClientHRAttemptContact     
    
if(@type='Completed')      
begin      
;with CTE as(Select tt.CandidateMasterId,tt.UserId,tt.CandidateLatestStatus,MAx(im.interviewdate) interviewdate,im.candidateEmail,im.IRFId 
From @TemporaryCandidateTableVariable tt join IRF on IRF.id=tt.IRFId  
join UserList ul on ul.id=tt.UserId    
join InterviewMeetings im on im.IRFId=IRF.IRFId and im.candidateEmail=ul.EmailId    
where ISNULL(im.isdeleted,0)=0  
group by tt.CandidateMasterId,tt.UserId,tt.CandidateLatestStatus,im.candidateEmail,im.IRFId)  
Select distinct cast(im.confirmationStatus as int) confirmationStatus, ic.MeetingStatus, ic.InterviewStatus,     
im.interviewDate,ice.InterviewStatus as InterviewStatusTwo,    
im.candidateEmail,im.candidateName,IRF.JDTiltle,IRF.JDTitleTwo,ip.NickName as InterviewerCodedName,techone.TechScoreone,techtwo.TechScoretwo,im.ClientHrContact,im.id,IRF.id as Irfintid,im.CodeSnippet,im.CodeSnippetTwo,IRF.IRFId    
from CTE join interviewMeetings im on im.Interviewdate=CTE.interviewdate and im.candidateEmail=CTE.candidateEmail and im.IRFId=CTE.IRFId  
join IRF on IRF.IRFId=im.IRFId  
join InterviewComments ic on ic.Interviewid=im.id    
left join InterviewCommentsExtension ice on ice.Interviewid=im.id   
left join UserList uli on uli.EmailId=im.interviewer    
left join InterviewerProfile ip on ip.userid=uli.id    
outer Apply (Select Sum(WeightageActual) as TechScoreone from TechSkillReport where skillset!=2 and Interviewid=im.id) techone    
outer Apply (Select Sum(WeightageActual) as TechScoretwo from TechSkillReport where skillset=2 and Interviewid=im.id) techtwo    
where CTE.CandidateLatestStatus in ('SELECTED', 'REJECTED')   
order by im.interviewdate desc    
end      
else if(@type='ColdCandidate')  
begin  
Select RTRIM(LTRIM(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,''))) as candidateName,ul.emailid as candidateEmail,ul.MobileNumber,LatestStatus.StatusValue,ca.ClientHRAttemptContact as ClientHrContact,IRF.IRFId  
from @TemporaryCandidateTableVariable tt join UserList ul on ul.id=tt.Userid  
join Candidate ca on ca.CandidateMasterId=tt.CandidateMasterId and ca.IRFId=tt.IRFId  
join IRF on IRF.id=ca.IRFId  
outer Apply (Select top 1 csm.StatusValue from CandidateStatus cs join CandidateStatusOptionsMst csm on csm.id=cs.Status  where cs.candidateid=tt.CandidateMasterId and cs.IRFId=tt.IRFId order by Date desc) LatestStatus  
where CandidateLatestStatus='COLDCANDIDATE'  
end  
else if(@type='NotScheduled')  
begin  
Select RTRIM(LTRIM(ISNULL(ul.FirstName,'')+' '+ISNULL(ul.LastName,''))) as candidateName,ul.emailid as candidateEmail,ul.MobileNumber,LatestStatus.StatusValue,ca.ClientHRAttemptContact as ClientHrContact,IRF.IRFId  
from @TemporaryCandidateTableVariable tt join UserList ul on ul.id=tt.Userid  
join Candidate ca on ca.CandidateMasterId=tt.CandidateMasterId and ca.IRFId=tt.IRFId  
join IRF on IRF.id=ca.IRFId  
outer Apply (Select top 1 csm.StatusValue from CandidateStatus cs join CandidateStatusOptionsMst csm on csm.id=cs.Status  where cs.candidateid=tt.CandidateMasterId and cs.IRFId=tt.IRFId order by Date desc) LatestStatus  
where CandidateLatestStatus='NOTSCHEDULED'  
end  
else if(@type='Reschedule')  
begin  
;with CTE as(Select tt.CandidateMasterId,tt.UserId,tt.CandidateLatestStatus,MAx(im.interviewdate) interviewdate,im.candidateEmail,im.IRFId From @TemporaryCandidateTableVariable tt join IRF on IRF.id=tt.IRFId  
join UserList ul on ul.id=tt.UserId    
join InterviewMeetings im on im.IRFId=IRF.IRFId and im.candidateEmail=ul.EmailId    
where ISNULL(im.isdeleted,0)=0  
group by tt.CandidateMasterId,tt.UserId,tt.CandidateLatestStatus,im.candidateEmail,im.IRFId)  
Select distinct cast(im.confirmationStatus as int) confirmationStatus, ic.MeetingStatus, ic.InterviewStatus,     
im.interviewDate,ice.InterviewStatus as InterviewStatusTwo,    
im.candidateEmail,im.candidateName,IRF.JDTiltle,IRF.JDTitleTwo,ip.NickName as InterviewerCodedName,techone.TechScoreone,techtwo.TechScoretwo,im.ClientHrContact,im.id,IRF.id as Irfintid,im.CodeSnippet,im.CodeSnippetTwo,IRF.IRFId    
from CTE join interviewMeetings im on im.Interviewdate=CTE.interviewdate and im.candidateEmail=CTE.candidateEmail and im.IRFId=CTE.IRFId  
join IRF on IRF.IRFId=im.IRFId  
join InterviewComments ic on ic.Interviewid=im.id    
left join InterviewCommentsExtension ice on ice.Interviewid=im.id   
left join UserList uli on uli.EmailId=im.interviewer    
left join InterviewerProfile ip on ip.userid=uli.id    
outer Apply (Select Sum(WeightageActual) as TechScoreone from TechSkillReport where skillset!=2 and Interviewid=im.id) techone    
outer Apply (Select Sum(WeightageActual) as TechScoretwo from TechSkillReport where skillset=2 and Interviewid=im.id) techtwo    
where CTE.CandidateLatestStatus in ('RESCHEDULE')   
order by im.interviewdate desc    
end  
else if(@type='Futureschedule')  
begin  
;with CTE as(Select tt.CandidateMasterId,tt.UserId,tt.CandidateLatestStatus,MAx(im.interviewdate) interviewdate,im.candidateEmail,im.IRFId From @TemporaryCandidateTableVariable tt join IRF on IRF.id=tt.IRFId  
join UserList ul on ul.id=tt.UserId    
join InterviewMeetings im on im.IRFId=IRF.IRFId and im.candidateEmail=ul.EmailId    
where ISNULL(im.isdeleted,0)=0  
group by tt.CandidateMasterId,tt.UserId,tt.CandidateLatestStatus,im.candidateEmail,im.IRFId)  
Select distinct cast(im.confirmationStatus as int) confirmationStatus, ic.MeetingStatus, ic.InterviewStatus,     
im.interviewDate,ice.InterviewStatus as InterviewStatusTwo,    
im.candidateEmail,im.candidateName,IRF.JDTiltle,IRF.JDTitleTwo,ip.NickName as InterviewerCodedName,techone.TechScoreone,techtwo.TechScoretwo,im.ClientHrContact,im.id,IRF.id as Irfintid,im.CodeSnippet,im.CodeSnippetTwo,IRF.IRFId    
from CTE join interviewMeetings im on im.Interviewdate=CTE.interviewdate and im.candidateEmail=CTE.candidateEmail and im.IRFId=CTE.IRFId  
join IRF on IRF.IRFId=im.IRFId  
join InterviewComments ic on ic.Interviewid=im.id    
left join InterviewCommentsExtension ice on ice.Interviewid=im.id   
left join UserList uli on uli.EmailId=im.interviewer    
left join InterviewerProfile ip on ip.userid=uli.id    
outer Apply (Select Sum(WeightageActual) as TechScoreone from TechSkillReport where skillset!=2 and Interviewid=im.id) techone    
outer Apply (Select Sum(WeightageActual) as TechScoretwo from TechSkillReport where skillset=2 and Interviewid=im.id) techtwo    
where CTE.CandidateLatestStatus in ('FUTURESCHEDULE') 
end 
else if(@type='Pending')  
begin  
;with CTE as(Select tt.CandidateMasterId,tt.UserId,tt.CandidateLatestStatus,MAx(im.interviewdate) interviewdate,im.candidateEmail,im.IRFId From @TemporaryCandidateTableVariable tt join IRF on IRF.id=tt.IRFId  
join UserList ul on ul.id=tt.UserId    
join InterviewMeetings im on im.IRFId=IRF.IRFId and im.candidateEmail=ul.EmailId    
where ISNULL(im.isdeleted,0)=0  
group by tt.CandidateMasterId,tt.UserId,tt.CandidateLatestStatus,im.candidateEmail,im.IRFId)  
Select distinct cast(im.confirmationStatus as int) confirmationStatus, ic.MeetingStatus, ic.InterviewStatus,     
im.interviewDate,ice.InterviewStatus as InterviewStatusTwo,    
im.candidateEmail,im.candidateName,IRF.JDTiltle,IRF.JDTitleTwo,ip.NickName as InterviewerCodedName,techone.TechScoreone,techtwo.TechScoretwo,im.ClientHrContact,im.id,IRF.id as Irfintid,im.CodeSnippet,im.CodeSnippetTwo,IRF.IRFId    
from CTE join interviewMeetings im on im.Interviewdate=CTE.interviewdate and im.candidateEmail=CTE.candidateEmail and im.IRFId=CTE.IRFId  
join IRF on IRF.IRFId=im.IRFId  
join InterviewComments ic on ic.Interviewid=im.id    
left join InterviewCommentsExtension ice on ice.Interviewid=im.id   
left join UserList uli on uli.EmailId=im.interviewer    
left join InterviewerProfile ip on ip.userid=uli.id    
outer Apply (Select Sum(WeightageActual) as TechScoreone from TechSkillReport where skillset!=2 and Interviewid=im.id) techone    
outer Apply (Select Sum(WeightageActual) as TechScoretwo from TechSkillReport where skillset=2 and Interviewid=im.id) techtwo    
where CTE.CandidateLatestStatus in ('PENDING') 
end 
END
GO