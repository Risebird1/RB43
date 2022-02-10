
IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_IsCandidateRejected]')
                      AND type IN ( N'FN' ))
BEGIN
    DROP FUNCTION dbo.fn_IsCandidateRejected
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Function fn_IsCandidateRejected(@userId int, @irfId int, @startDate date)
Returns bit
as
Begin

declare @candidateEmail varchar(150), @irfName varchar(150)

set @candidateEmail = (select EmailId from UserList where id = @userId)
set @irfName = (select IRFId from IRF where id = @irfId)

DECLARE @TemporaryStatus TABLE (      
		confirmationStatus int,
		InterviewStatus int
);   

INSERT INTO @TemporaryStatus      
select top 1 cast(im.confirmationStatus as int), ic.InterviewStatus
from InterviewMeetings im 
join InterviewComments ic on im.id=ic.InterviewId 
where im.candidateEmail=@candidateEmail and im.IRFId = @irfName 
and cast(im.interviewDate as date) >= @startDate  
order by im.interviewDate desc

declare @confirmationStatus int, @interviewStatus int

set @confirmationStatus = (select top 1 confirmationStatus from @TemporaryStatus)
set @interviewStatus = (select top 1 InterviewStatus from @TemporaryStatus)

declare @isRejected bit

if(@confirmationStatus = 1 and @interviewStatus in (1,3))
begin
	set @isRejected=1
end
else
begin
	set @isRejected=0
end
return @isRejected

end





