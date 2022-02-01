
IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_IsCandidateNeverScheduled]')
                      AND type IN ( N'FN' ))
BEGIN
    DROP FUNCTION dbo.fn_IsCandidateNeverScheduled
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Function fn_IsCandidateNeverScheduled(@userId int, @irfId int, @startDate date)
Returns bit
as
Begin

declare @candidateEmail varchar(150), @irfName varchar(150), @resultId int

set @candidateEmail = (select EmailId from UserList where id = @userId)
set @irfName = (select IRFId from IRF where id = @irfId)

set @resultId = 0

set @resultId = (select top 1 im.id 
from InterviewMeetings im 
where im.candidateEmail=@candidateEmail and im.IRFId = @irfName 
and cast(im.interviewDate as date) >= @startDate  
order by im.interviewDate desc)

declare @isNeverScheduled bit


if(@resultId = 0)
begin
	set @isNeverScheduled=1
end
else
begin
	set @isNeverScheduled=0
end

return @isNeverScheduled

end





