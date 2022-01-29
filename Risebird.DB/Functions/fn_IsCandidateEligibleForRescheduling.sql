
IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_IsCandidateEligibleForRescheduling]')
                      AND type IN ( N'FN' ))
BEGIN
    DROP FUNCTION dbo.fn_IsCandidateEligibleForRescheduling
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Function fn_IsCandidateEligibleForRescheduling(@userId int, @irfId int, @startDate datetime)
Returns bit
as
Begin

declare @candidateEmail varchar(150), @irfName varchar(150)

set @candidateEmail = (select EmailId from UserList where id = @userId)
set @irfName = (select IRFId from IRF where id = @irfId)


declare @isNoShow bit

if exists (select im.id from InterviewMeetings im join InterviewComments ic on im.id=ic.InterviewId where im.candidateEmail=@candidateEmail and im.IRFId = @irfName and im.interviewDate >= @startDate and ic.MeetingStatus in (1,5) and im.confirmationStatus=1)
begin
	set @isNoShow=1
end
else
begin
	set @isNoShow=0
end

declare @isDropped bit

if exists (select im.id from InterviewMeetings im join InterviewComments ic on im.id=ic.InterviewId where im.candidateEmail=@candidateEmail and im.IRFId = @irfName and im.interviewDate >= @startDate and ic.MeetingStatus = 3 and im.confirmationStatus=1)
begin
	set @isDropped=1
end
else
begin
	set @isDropped=0
end

declare @isCancelled bit

if exists (select im.id from InterviewMeetings im  where im.candidateEmail=@candidateEmail and im.IRFId = @irfName and im.interviewDate >= @startDate and im.confirmationStatus=2)
begin
	set @isCancelled=1
end
else
begin
	set @isCancelled=0
end


declare @isCompletedInAnyWayOtherThanAbove bit

if exists (select im.id from InterviewMeetings im join InterviewComments ic on im.id=ic.InterviewId where im.candidateEmail=@candidateEmail and im.IRFId = @irfName and im.interviewDate >= @startDate and im.confirmationStatus=1 and (ic.MeetingStatus in (2,4,6,7,8) or ic.MeetingStatus is null))
begin
	set @isCompletedInAnyWayOtherThanAbove=1
end
else
begin
	set @isCompletedInAnyWayOtherThanAbove=0
end

declare @isEligibleForReSchedule bit
set @isEligibleForReSchedule =  0

if((@isNoShow=1 or @isDropped=1 or @isCancelled=1) and (@isCompletedInAnyWayOtherThanAbove=0))
begin
	set @isEligibleForReSchedule = 1
end

return @isEligibleForReSchedule

end





