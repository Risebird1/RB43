
IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_HasCandidateReScheduled]')
                      AND type IN ( N'FN' ))
BEGIN
    DROP FUNCTION dbo.fn_HasCandidateReScheduled
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Function fn_HasCandidateReScheduled(@candidateEmail varchar(150), @irfId varchar(100), @startDate datetime)
Returns bit
as
Begin

declare @isReScheduled bit, @isCancelled bit, @isProxyOrNoShowOrDropped bit, @isSelectedorRejectedorBorderLine bit
set @isReScheduled=0
set @isCancelled=0
set @isProxyOrNoShowOrDropped=0
set @isSelectedorRejectedorBorderLine=0



if exists(select id from InterviewMeetings where candidateEmail =@candidateEmail and IRFId =@irfId and confirmationStatus=2 and interviewDate >= @startDate)
begin
	set @isCancelled =1
end

if exists(select im.id from InterviewMeetings im join InterviewComments ic on ic.InterviewId=im.id where candidateEmail =@candidateEmail and IRFId =@irfId and confirmationStatus=1 and interviewDate >= @startDate and (ic.MeetingStatus in (1,3) or ic.InterviewStatus=1))
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
 return @isReScheduled



End