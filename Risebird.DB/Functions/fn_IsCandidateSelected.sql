
IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_IsCandidateSelected]')
                      AND type IN ( N'FN' ))
BEGIN
    DROP FUNCTION dbo.fn_IsCandidateSelected
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Function fn_IsCandidateSelected(@userId int, @irfId int, @startDate date)
Returns bit
as
Begin

declare @candidateEmail varchar(150), @irfName varchar(150), @resultId int

set @candidateEmail = (select EmailId from UserList where id = @userId)
set @irfName = (select IRFId from IRF where id = @irfId)

set @resultId = 0

set @resultId = (select top 1 im.id 
from InterviewMeetings im 
join InterviewComments ic on im.id=ic.InterviewId 
where im.candidateEmail=@candidateEmail and im.IRFId = @irfName 
and cast(im.interviewDate as date) >= @startDate and ic.InterviewStatus in (2,4) 
and im.confirmationStatus=1
order by im.interviewDate desc)

declare @isSelected bit

if(@resultId>0)
begin
	set @isSelected=1
end
else
begin
	set @isSelected=0
end
return @isSelected

end





