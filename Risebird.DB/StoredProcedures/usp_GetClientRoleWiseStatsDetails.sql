IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetClientRoleWiseStatsDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientRoleWiseStatsDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO      
                      
CREATE PROCEDURE usp_GetClientRoleWiseStatsDetails     
                  
@timeNow datetime,
@irfId int = null,
@type varchar(30),
@roleType varchar(30),
@clientUserId int

AS          
BEGIN  

--declare @yesterdayDate date = cast(dateadd(day, -1, @timeNow) as date)
declare @IRFName varchar(30)
if(@irfId is not null)
begin
	set @IRFName = (select IRFId from IRF where id = @irfId)
end

declare @clientEmail varchar(100) = (select EmailId from UserList where id = @clientUserId)

DECLARE @tbl_RelevantInterviews TABLE (                    
	id int,
	interviewDate datetime,
	candidateEmail varchar(200),
	SkillChannel varchar(200),
	interviewer varchar(200)
)

DECLARE @tbl_RelevantCandidates TABLE (                    
	CandidateUserId int,
	CandidateEmail varchar(200),
	IRFId int,
	IRFName varchar(30)	
)

INSERT INTO @tbl_RelevantCandidates 
select cm.UserId, ul.EmailId as CandidateEmail, ca.IRFId, irf.IRFId as IRFName
from Candidate ca join CandidatesMaster cm
on cm.id = ca.CandidateMasterId
join UserList ul on ul.id = cm.UserId
join IRF on irf.id = ca.IRFId
where (@irfId is null or ca.IRFId = @irfId)
group by cm.UserId, ul.EmailId, ca.IRFId, irf.IRFId


INSERT INTO @tbl_RelevantInterviews    
select im.id, im.interviewDate, im.candidateEmail, im.IRFId, im.interviewer from @tbl_RelevantCandidates trc 
join InterviewMeetings im on im.candidateEmail = trc.CandidateEmail and im.IRFId = trc.IRFName
and  (@irfId is null or im.IRFId = @IRFName) 
where im.confirmationStatus = 1 
and ((im.staffingAgency = @clientEmail and @roleType = 'Staffing Agency') or (im.ClientHrContact = @clientEmail and @roleType = 'ClientHR'))


DECLARE @tbl_InterviewerJoinTime TABLE (                    
	interviewId int,
	interviewer varchar(200),
	JoiningTime datetime
)

INSERT INTO @tbl_InterviewerJoinTime
select interviewid, imjt.interviewer, max(Joinedon) from InterviewMeetingsJoinTime imjt
join @tbl_RelevantInterviews tri on tri.id = imjt.interviewid and tri.interviewer = imjt.interviewer
group by interviewid, imjt.interviewer


DECLARE @tbl_ResultInterviews TABLE (                    
	interviewId int,
	interviewDate datetime,
	candidateEmail varchar(200),
	SkillChannel varchar(200),
	Client varchar(50),
	MeetingStatus int,
	InterviewStatus int,
	ResultType varchar(30),
	InterviewerJoinTime datetime
)


INSERT INTO @tbl_ResultInterviews    
select im.id, im.interviewDate, im.candidateEmail, im.SkillChannel, cl.[Name], ic.MeetingStatus, 
ic.InterviewStatus, @type, tijt.JoiningTime
from @tbl_RelevantInterviews im
left join InterviewComments ic on ic.InterviewId = im.id
join IRF on irf.IRFId = im.SkillChannel
join CLient cl on cl.id = irf.ClientId
left join @tbl_InterviewerJoinTime tijt on tijt.interviewId = im.id
where (@type =  'all') 
or (@type = 'completed' and ic.MeetingStatus in (4, 6, 7, 8))
or (@type = 'selected' and (ic.InterviewStatus = 2) and (ic.MeetingStatus = 4 or ic.MeetingStatus = 6))
or (@type = 'borderline' and (ic.InterviewStatus = 4) and (ic.MeetingStatus = 4 or ic.MeetingStatus = 8))
or (@type = 'rejected' and (ic.InterviewStatus = 3) and (ic.MeetingStatus = 4  or ic.MeetingStatus = 7))
or (@type = 'proxy' and ic.InterviewStatus = 1 and ic.MeetingStatus = 4)
or (@type = 'noshowinterviewer' and  (ic.MeetingStatus = 5))
or (@type = 'noshowcandidate' and  (ic.MeetingStatus = 1))
or (@type = 'dropped' and ic.MeetingStatus = 3)
or (@type = 'pending' and ((im.interviewDate < @timeNow) and ((ic.MeetingStatus = 4 and ic.InterviewStatus is null) or (ic.MeetingStatus is null) or ic.MeetingStatus = 2)))
or (@type = 'scheduled' and ic.MeetingStatus = 2 and im.interviewDate > @timeNow)


    
	select * from @tbl_ResultInterviews

END        
  
GO           


