IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAssessmentGroupIRFList'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAssessmentGroupIRFList
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
CREATE PROCEDURE usp_GetAssessmentGroupIRFList 
@Assessmentid int
AS
BEGIN
DECLARE @tbl_TotalMeetings TABLE (          
    IRFId INT,          
    TotalMeetings int           
);          
INSERT INTO @tbl_TotalMeetings          
  Select ca.IRFId, Count(ca.id) as TotalMeetings from Candidate ca          
join CandidatesMaster cm on ca.CandidateMasterId=cm.id           
group by ca.IRFId   

DECLARE @tbl_SelectedMeetings TABLE (          
    IRFId INT,          
    SelectedMeetings int           
);          
INSERT INTO @tbl_SelectedMeetings          
select irf.id as IRFId, count(im.id) as SelectedMeetings          
from irf left join InterviewMeetings im on irf.IRFId= im.IRFId           
left join InterviewComments ic on im.id= ic.InterviewId          
left join QCFeedbackReport qcf on qcf.InterviewId = im.id          
inner join UserList ul on ul.EmailId = im.candidateEmail           
inner join CandidatesMaster cm on ul.id =cm.UserId          
inner join Candidate ca on (cm.id=ca.CandidateMasterId and ca.IRFId=irf.id)          
where (ic.InterviewStatus= 2 or ic.InterviewStatus=4)       
group by irf.id    
union    
select irf.id as IRFId, count(im.id) as SelectedMeetings          
from irf left join InterviewMeetings im on irf.IRFId= im.IRFId           
left join InterviewComments ic on im.id= ic.InterviewId          
inner join UserList ul on ul.EmailId = im.candidateEmail           
inner join CandidatesMaster cm on ul.id =cm.UserId          
inner join Candidate ca on (cm.id=ca.CandidateMasterId and ca.IRFId=irf.id)          
where (ic.InterviewStatus= 2 or ic.InterviewStatus=4) and ISNULL(irf.IsQCRequired,0)=0    
group by irf.id    

DECLARE @tbl_RejectedMeetings TABLE (          
    IRFId INT,          
    RejectedMeetings int           
);          
INSERT INTO @tbl_RejectedMeetings          
select irf.id as IRFId, count(im.id) as RejectedMeetings          
from irf left join InterviewMeetings im on irf.IRFId= im.IRFId           
left join InterviewComments ic on im.id= ic.InterviewId          
inner join UserList ul on ul.EmailId = im.candidateEmail           
inner join CandidatesMaster cm on ul.id =cm.UserId          
inner join Candidate ca on (cm.id=ca.CandidateMasterId and ca.IRFId=irf.id)          
where (ic.InterviewStatus= 1 or ic.InterviewStatus=3)
group by irf.id    

DECLARE @tbl_CompletedMeetings TABLE (          
    IRFId INT,          
    CompletedMeetings int           
);          
INSERT INTO @tbl_CompletedMeetings          
select irf.id as IRFId, count(im.id) as CompletedMeetings           
from irf left join InterviewMeetings im on irf.IRFId = im.IRFId           
left join InterviewComments ic on ic.InterviewId= im.id           
inner join UserList ul on ul.EmailId = im.candidateEmail           
inner join CandidatesMaster cm on ul.id =cm.UserId          
inner join Candidate ca on (cm.id=ca.CandidateMasterId and ca.IRFId=irf.id)          
where ic.MeetingStatus=4 and im.confirmationStatus=1         
group by irf.id   

DECLARE @tbl_ScheduledMeetings TABLE (          
    IRFId INT,          
    ScheduledMeetings int           
);          
INSERT INTO @tbl_ScheduledMeetings          
select irf.id as IRFId, count(im.id) as ScheduledMeetings           
from irf left join InterviewMeetings im on irf.IRFId = im.IRFId           
inner join UserList ul on ul.EmailId = im.candidateEmail           
inner join CandidatesMaster cm on ul.id =cm.UserId          
inner join Candidate ca on (cm.id=ca.CandidateMasterId and ca.IRFId=irf.id)          
where im.confirmationStatus=1         
group by irf.id   


Select distinct IRF.id,IRF.IRFId,PrimarySkillInvite,ttm.TotalMeetings,tsm.SelectedMeetings,ttr.RejectedMeetings,ttc.CompletedMeetings,tts.ScheduledMeetings
from IRF left join InterviewMeetings im on im.IRFId=irf.IRFId 
left join Candidate ca on ca.IRFId=IRF.id         
left join CandidatesMaster cm on ca.CandidateMasterId=cm.id 
left join @tbl_SelectedMeetings tsm on tsm.IRFId=IRF.id
left join @tbl_TotalMeetings ttm on ttm.IRFId=IRF.id
left join @tbl_RejectedMeetings ttr on ttr.IRFId=IRF.id
left join @tbl_CompletedMeetings ttc on ttc.IRFId=IRF.id
left join @tbl_ScheduledMeetings tts on tts.IRFId=IRF.id
where IRF.AssessmentGroupId=@Assessmentid
END
GO