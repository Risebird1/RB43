
IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_HasCandidateScheduledFutureInterview]')
                      AND type IN ( N'FN' ))
BEGIN
    DROP FUNCTION dbo.fn_HasCandidateScheduledFutureInterview
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Function fn_HasCandidateScheduledFutureInterview(@candidateEmail varchar(150), @irfId varchar(100), @timeNow datetime)
Returns bit
as
Begin

declare @isFutureScheduled bit
set @isFutureScheduled=0

if exists(select id from InterviewMeetings where candidateEmail =@candidateEmail and IRFId =@irfId and confirmationStatus=1 and interviewDate >= @timeNow )
begin
	set @isFutureScheduled=1
end
 return @isFutureScheduled

End