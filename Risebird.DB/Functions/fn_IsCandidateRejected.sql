
IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_IsCandidateRejected]')
                      AND type IN ( N'FN' ))
BEGIN
    DROP FUNCTION dbo.fn_IsCandidateRejected
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Function fn_IsCandidateRejected(@candidateEmail varchar(150), @irfId varchar(100), @startDate datetime)
Returns bit
as
Begin

declare @isRejected bit
set @isRejected=0

if exists(select im.id from InterviewMeetings im join InterviewComments ic on ic.InterviewId = im.id    
 where im.candidateEmail=@candidateEmail and im.IRFId =@irfId and im.confirmationStatus=1 and ic.InterviewStatus= 3 and im.interviewDate >= @startDate)
begin
	set @isRejected=1
end
 return @isRejected

End