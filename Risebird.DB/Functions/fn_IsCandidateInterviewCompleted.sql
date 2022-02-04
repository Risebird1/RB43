
IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_IsCandidateInterviewCompleted]')
                      AND type IN ( N'FN' ))
BEGIN
    DROP FUNCTION dbo.fn_IsCandidateInterviewCompleted
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Function fn_IsCandidateInterviewCompleted(@userId int, @irfId int, @startDate date)
Returns bit
as
Begin

declare @candidateEmail varchar(150), @irfName varchar(150)--, @result int

set @candidateEmail = (select EmailId from UserList where id = @userId)
set @irfName = (select IRFId from IRF where id = @irfId)

DECLARE @TemporaryStatus TABLE (      
		confirmationStatus int,
		MeetingStatus int
);   

INSERT INTO @TemporaryStatus      
select top 1 cast(im.confirmationStatus as int), ic.MeetingStatus
from InterviewMeetings im 
join InterviewComments ic on im.id=ic.InterviewId 
where im.candidateEmail=@candidateEmail and im.IRFId = @irfName 
and cast(im.interviewDate as date) >= @startDate 
order by im.interviewDate desc


declare @confirmationStatus int, @meetingStatus int

set @confirmationStatus = (select top 1 confirmationStatus from @TemporaryStatus)
set @meetingStatus = (select top 1 MeetingStatus from @TemporaryStatus)

declare @isCompleted bit

if(@confirmationStatus=1 and @meetingStatus in (4,6,7,8))
begin
	set @isCompleted=1
end
else
begin
	set @isCompleted=0
end
return @isCompleted

end





