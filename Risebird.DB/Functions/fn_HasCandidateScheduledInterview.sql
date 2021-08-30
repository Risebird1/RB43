
IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_HasCandidateScheduledInterview]')
                      AND type IN ( N'FN' ))
BEGIN
    DROP FUNCTION dbo.fn_HasCandidateScheduledInterview
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Function fn_HasCandidateScheduledInterview(@candidateEmail varchar(150), @irfId varchar(100), @startDate datetime)
Returns bit
as
Begin

declare @isScheduled bit, @hasMeetingStatusOrInterviewStatusOtherthanScheduled bit, @result bit

set @isScheduled=0
set @hasMeetingStatusOrInterviewStatusOtherthanScheduled=0
set @result=0

if exists(select im.id from InterviewMeetings im join InterviewComments ic on ic.InterviewId = im.id where candidateEmail =@candidateEmail and IRFId =@irfId and confirmationStatus=1 and ((ic.MeetingStatus is not null and ic.MeetingStatus not in (2)) or (ic.InterviewStatus is not null)) and interviewDate >= @startDate)
begin
	set @hasMeetingStatusOrInterviewStatusOtherthanScheduled =1
end

if exists(select id from InterviewMeetings where candidateEmail =@candidateEmail and IRFId =@irfId and confirmationStatus=1 and interviewDate >= @startDate )
begin
	set @isScheduled=1
end

if(@isScheduled=1 and @hasMeetingStatusOrInterviewStatusOtherthanScheduled=0)
begin
	set @result=1
end
 return @result

End