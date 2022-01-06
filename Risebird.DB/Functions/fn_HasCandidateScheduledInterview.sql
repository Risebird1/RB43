
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

declare @isScheduled bit--,  @result bit, @hasMeetingStatusOrInterviewStatusOtherthanScheduled bit,

set @isScheduled=0
--set @hasMeetingStatusOrInterviewStatusOtherthanScheduled=0
--set @result=0

declare @isReScheduled bit, @isCancelled bit, @isProxyOrNoShowOrDropped bit, @isSelectedorRejectedorBorderLine bit
set @isReScheduled=0
set @isCancelled=0
set @isProxyOrNoShowOrDropped=0
set @isSelectedorRejectedorBorderLine=0



if exists(select id from InterviewMeetings where candidateEmail =@candidateEmail and IRFId =@irfId and confirmationStatus=2 and interviewDate >= @startDate)
begin
	set @isCancelled =1
end

if exists(select im.id from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id where candidateEmail =@candidateEmail and IRFId =@irfId and confirmationStatus=1 and interviewDate >= @startDate and (ic.MeetingStatus in (1,3,5) or ic.InterviewStatus=1))
begin
	set @isProxyOrNoShowOrDropped =1
end

if exists(select im.id from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id where candidateEmail =@candidateEmail and IRFId =@irfId and confirmationStatus=1 and interviewDate >= @startDate and ic.InterviewStatus in (2,4))
begin
	set @isSelectedorRejectedorBorderLine =1
end

if((@isCancelled=1 or @isProxyOrNoShowOrDropped=1) and @isSelectedorRejectedorBorderLine=0)
begin
	set @isReScheduled=1
end

declare @isCompleted bit
set @isCompleted=0

if exists(select im.id from InterviewMeetings im join InterviewComments ic on ic.InterviewId = im.id where im.candidateEmail =@candidateEmail and im.IRFId =@irfId and im.confirmationStatus=1 and ic.MeetingStatus=4 and im.interviewDate >= @startDate)
begin
	set @isCompleted=1
end

if(@isReScheduled=1 or @isCompleted=1)
begin
	set @isScheduled=1
end


--if exists(select im.id from InterviewMeetings im join InterviewComments ic on ic.InterviewId = im.id where candidateEmail =@candidateEmail and IRFId =@irfId and confirmationStatus=1 and ((ic.MeetingStatus is not null and ic.MeetingStatus not in (2)) or (ic.InterviewStatus is not null)) and interviewDate >= @startDate)
--begin
--	set @hasMeetingStatusOrInterviewStatusOtherthanScheduled =1
--end

--if exists(select id from InterviewMeetings where candidateEmail =@candidateEmail and IRFId =@irfId and confirmationStatus=1 and interviewDate >= @startDate )
--begin
--	set @isScheduled=1
--end

--if(@isScheduled=1 and @hasMeetingStatusOrInterviewStatusOtherthanScheduled=0)
--begin
--	set @result=1
--end
 return @isScheduled

End