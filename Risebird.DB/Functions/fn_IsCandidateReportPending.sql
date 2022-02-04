
IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_IsCandidateReportPending]')
                      AND type IN ( N'FN' ))
BEGIN
    DROP FUNCTION dbo.fn_IsCandidateReportPending
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Function fn_IsCandidateReportPending(@userId int, @irfId int, @startDate date, @timeNow datetime)
Returns bit
as
Begin

declare @candidateEmail varchar(150), @irfName varchar(150)

set @candidateEmail = (select EmailId from UserList where id = @userId)
set @irfName = (select IRFId from IRF where id = @irfId)


DECLARE @TemporaryStatus TABLE (      
	confirmationStatus int,
	MeetingStatus int
);   

INSERT INTO @TemporaryStatus      
select top 1 cast(im.confirmationStatus as int), ic.MeetingStatus
from InterviewMeetings im 
left join InterviewComments ic on im.id=ic.InterviewId 
where im.candidateEmail=@candidateEmail and im.IRFId = @irfName 
and cast(im.interviewDate as date) >= @startDate 
and im.interviewDate < @timeNow
order by im.interviewDate desc


declare @confirmationStatus int, @meetingStatus int

set @confirmationStatus = (select top 1 confirmationStatus from @TemporaryStatus)
set @meetingStatus = (select top 1 MeetingStatus from @TemporaryStatus)

declare @isCandidateReportPending bit
set @isCandidateReportPending =  0

if(@confirmationStatus = 1 and (@meetingStatus=2 or @meetingStatus is null))
begin
	set @isCandidateReportPending = 1
end

return @isCandidateReportPending

End





