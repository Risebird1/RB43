
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

declare @interviewId int
set @interviewId = 0
set @interviewId = (select top 1 id from InterviewMeetings where candidateEmail =@candidateEmail and IRFId =@irfId and interviewDate >= @startDate order by interviewDate desc)



declare @isReSchedule bit, @confirmationStatus int, @meetingStatus int
set @isReSchedule =  0


if(@interviewId is not null and @interviewId!=0)
begin
	set @confirmationStatus = (select confirmationStatus from InterviewMeetings where id = @interviewId)
	if(@confirmationStatus =2)
	begin
		 set @isReSchedule=1
		 return @isReSchedule
	end
	set @meetingStatus = (select MeetingStatus from InterviewComments where InterviewId = @interviewId)
	if(@meetingStatus in (1,3,5))
	begin
		set @isReSchedule=1
		return @isReSchedule
	end

end

return @isReSchedule

end





