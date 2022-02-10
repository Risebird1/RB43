
IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_CandidateLatestStatusForIrfStats]')
                      AND type IN ( N'FN' ))
BEGIN
    DROP FUNCTION dbo.fn_CandidateLatestStatusForIrfStats
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION fn_CandidateLatestStatusForIrfStats(@userId int, @irfId int, @startDate date, @timeNow datetime)
RETURNS varchar(20)
AS
BEGIN

declare @candidateEmail varchar(150), @irfName varchar(150)

set @candidateEmail = (select EmailId from UserList where id = @userId)
set @irfName = (select IRFId from IRF where id = @irfId)

DECLARE @TemporaryStatus TABLE (      
		confirmationStatus int,
		MeetingStatus int,
		InterviewStatus int,
		interviewDate datetime
);   

INSERT INTO @TemporaryStatus      
select top 1 cast(im.confirmationStatus as int), ic.MeetingStatus, ic.InterviewStatus, im.interviewDate
from InterviewMeetings im 
left join InterviewComments ic on im.id=ic.InterviewId 
where im.candidateEmail=@candidateEmail and im.IRFId = @irfName 
and cast(im.interviewDate as date) >= @startDate  
order by im.interviewDate desc

declare @confirmationStatus int, @meetingStatus int, @interviewStatus int, @interviewDate datetime, @result varchar(20)

set @confirmationStatus = (select top 1 confirmationStatus from @TemporaryStatus)
set @meetingStatus = (select top 1 MeetingStatus from @TemporaryStatus)
set @interviewStatus = (select top 1 InterviewStatus from @TemporaryStatus)
set @interviewDate = (select top 1 interviewDate from @TemporaryStatus)

--case no entry in interviewmeetings table
if not exists(select top 1 confirmationStatus from @TemporaryStatus)
begin
	 set @result = 'NOTSCHEDULED'
end

--selected condition
else if(@confirmationStatus=1 and @interviewStatus in (2,4))
begin
	set @result = 'SELECTED'
end

--case for rejected
else if(@confirmationStatus=1 and @interviewStatus in (1,3))
begin
	set @result = 'REJECTED'
end

--case for eligibility for rescheduling
else if(@confirmationStatus = 2 or @meetingStatus in (1,3,5))
begin
	set @result = 'RESCHEDULE'
end

--case for scheduled in the future
else if(@interviewDate> @timeNow and @confirmationStatus = 1)
begin
	set @result = 'FUTURESCHEDULE'
end

--case for report pending
else if(@interviewDate < @timeNow and @confirmationStatus = 1 and (@meetingStatus=2 or @meetingStatus is null))
begin
	set @result = 'PENDING'
end

return @result
END

