/****** Object:  UserDefinedFunction [dbo].[fn_IsCandidateScheduledInfuture]    Script Date: 2/7/2022 5:41:10 AM ******/
IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_IsCandidateScheduledInfuture]')
                      AND type IN ( N'FN' ))
BEGIN
    DROP FUNCTION dbo.fn_IsCandidateScheduledInfuture
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create Function [dbo].[fn_IsCandidateScheduledInfuture](@userId int, @irfId int, @timeNow datetime)
Returns bit
as
Begin

declare @candidateEmail varchar(150), @irfName varchar(150), @result int

set @candidateEmail = (select EmailId from UserList where id = @userId)
set @irfName = (select IRFId from IRF where id = @irfId)

set @result = 0

set @result = (select top 1 im.id
from InterviewMeetings im 
where im.candidateEmail=@candidateEmail and im.IRFId = @irfName 
and im.interviewDate >= @timeNow 
and im.confirmationStatus=1
order by im.interviewDate desc)

declare @isFutureScheduled bit

if(@result > 0)
begin
	set @isFutureScheduled=1
end
else
begin
	set @isFutureScheduled=0
end
return @isFutureScheduled

end





GO


