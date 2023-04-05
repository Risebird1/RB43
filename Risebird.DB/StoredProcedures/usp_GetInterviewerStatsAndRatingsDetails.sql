IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviewerStatsAndRatingsDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerStatsAndRatingsDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO      
                      
CREATE PROCEDURE usp_GetInterviewerStatsAndRatingsDetails     
                  
@timeNow datetime,
@irfId int = null,
@type varchar(30),
@interviewerUserId int,
@clientEmailId varchar(100) = null

AS          
BEGIN  

declare @yesterdayDate date = cast(dateadd(day, -1, @timeNow) as date)
declare @IRFName varchar(30)
if(@irfId is not null)
begin
	set @IRFName = (select IRFId from IRF where id = @irfId)
end

declare @interviewerEmail varchar(100) = (select EmailId from UserList where id = @interviewerUserId)

DECLARE @tbl_RelevantInterviews TABLE (                    
	id int,
	interviewDate datetime,
	--interviewer varchar(200),
	candidateEmail varchar(200),
	SkillChannel varchar(200)
)

INSERT INTO @tbl_RelevantInterviews    
select im.id, interviewDate, candidateEmail, IRFId from InterviewMeetings im --, interviewer
where ((cast(im.interviewDate as date)>= @yesterdayDate and @irfId is null) or @irfId is not null) 
and confirmationStatus = 1 and (@IRFName is null or im.IRFId = @IRFName)
and interviewer = @interviewerEmail
and (@clientEmailId is null or (im.ClientHrContact = @clientEmailId or im.staffingAgency = @clientEmailId))


DECLARE @tbl_InterviewerJoinTime TABLE (                    
	interviewId int,
	interviewer varchar(200),
	JoiningTime datetime
)

INSERT INTO @tbl_InterviewerJoinTime
select interviewid, ijt.interviewer, max(Joinedon) from InterviewMeetingsJoinTime ijt
join InterviewMeetings im on im.id = ijt.interviewid
where ijt.interviewer = @interviewerEmail
and (@clientEmailId is null or (im.ClientHrContact = @clientEmailId or im.staffingAgency = @clientEmailId))
group by interviewid, ijt.interviewer


DECLARE @tbl_ResultInterviews TABLE (                    
	interviewId int,
	interviewDate datetime,
	candidateEmail varchar(200),
	SkillChannel varchar(200),
	Client varchar(50),
	MeetingStatus int,
	InterviewStatus int,
	ResultType varchar(30),
	InterviewerJoinTime datetime,
	irfId int
)


INSERT INTO @tbl_ResultInterviews    
select im.id, im.interviewDate, im.candidateEmail, im.SkillChannel, cl.[Name], ic.MeetingStatus, 
ic.InterviewStatus, @type, tijt.JoiningTime,IRF.id as irfId
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
or (@type = 'noshowallinclusive' and  (ic.MeetingStatus = 1 or ic.MeetingStatus = 5))
or (@type = 'dropped' and ic.MeetingStatus = 3)
or (@type = 'pending' and ((im.interviewDate < @timeNow) and ((ic.MeetingStatus = 4 and ic.InterviewStatus is null) or (ic.MeetingStatus is null) or ic.MeetingStatus = 2)))
or (@type = 'scheduled' and ic.MeetingStatus = 2 and im.interviewDate > @timeNow)


    
	select * from @tbl_ResultInterviews

END        
  
GO           


