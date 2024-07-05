IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetIRFMutliStackInterviewStatusDetailedView'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFMutliStackInterviewStatusDetailedView
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_GetIRFMutliStackInterviewStatusDetailedView  
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

if(@type='Completed')  
begin  
INSERT INTO @TemporaryCandidateTableVariable                      
select ca.IRFId, ca.CandidateMasterId, cm.UserId,                  
dbo.fn_CandidateLatestStatusForIrfMultiStackStats(cm.UserId, ca.IRFId, @startdate, @timenow)                
from  Candidate ca                 
join CandidateStatus cs on  (ca.IRFId = cs.IRFId and ca.CandidateMasterId = cs.CandidateId)                 
join CandidatesMaster cm on cm.id=ca.CandidateMasterId    
join IRF tt on tt.id=ca.IRFId 
where tt.IRFId=@irfname
group by ca.IRFId, ca.CandidateMasterId,cm.UserId                
                
 Select distinct cast(im.confirmationStatus as int) confirmationStatus, ic.MeetingStatus, ic.InterviewStatus, 
im.interviewDate,ice.InterviewStatus as InterviewStatusTwo,
im.candidateEmail,im.candidateName,IRF.JDTiltle,IRF.JDTitleTwo,ip.NickName as InterviewerCodedName,techone.TechScoreone,techtwo.TechScoretwo,im.ClientHrContact
 From IRF join @TemporaryCandidateTableVariable tt on tt.IRFId=IRF.id  
 join UserList ul on ul.id=tt.UserId
 join InterviewMeetings im on im.IRFId=IRF.IRFId and im.candidateEmail=ul.EmailId
 join InterviewComments ic on ic.Interviewid=im.id
 left join InterviewCommentsExtension ice on ice.Interviewid=im.id  
 left join UserList uli on uli.EmailId=im.interviewer
 left join InterviewerProfile ip on ip.userid=uli.id
 outer Apply (Select Sum(WeightageActual) as TechScoreone from TechSkillReport where skillset!=2 and Interviewid=im.id) techone
 outer Apply (Select Sum(WeightageActual) as TechScoretwo from TechSkillReport where skillset=2 and Interviewid=im.id) techtwo
 where tt.CandidateLatestStatus in ('BOTHSELECTED', 'BOTHREJECTED','SELECTEDNREJECTED','REJECTEDNSELECTED') and confirmationStatus=1 and Isnull(im.RedoInterview,0)=0
 and ic.InterviewStatus in(1,2,3,4,5)
 order by candidateEmail desc
end  
END  
 GO