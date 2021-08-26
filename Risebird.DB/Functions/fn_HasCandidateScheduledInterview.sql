
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

declare @isScheduled bit
set @isScheduled=0

if exists(select id from InterviewMeetings where candidateEmail =@candidateEmail and IRFId =@irfId and confirmationStatus=1 and interviewDate >= @startDate )
begin
	set @isScheduled=1
end
 return @isScheduled

End