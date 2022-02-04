
IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_IsCandidateEverScheduledTillNow]')
                      AND type IN ( N'FN' ))
BEGIN
    DROP FUNCTION dbo.fn_IsCandidateEverScheduledTillNow
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Function fn_IsCandidateEverScheduledTillNow(@userId int, @irfId int, @startDate date, @timeNow datetime)
Returns bit
as
Begin

declare @candidateEmail varchar(150), @irfName varchar(150)

set @candidateEmail = (select EmailId from UserList where id = @userId)
set @irfName = (select IRFId from IRF where id = @irfId)

declare @isEverScheduledTillNow bit

if exists(select im.id
from InterviewMeetings im 
where im.candidateEmail=@candidateEmail and im.IRFId = @irfName 
and cast(im.interviewDate as date) >= @startDate and im.interviewDate < @timeNow)
begin
	set @isEverScheduledTillNow=1
end

else
begin
	set @isEverScheduledTillNow=0
end

return @isEverScheduledTillNow

end





