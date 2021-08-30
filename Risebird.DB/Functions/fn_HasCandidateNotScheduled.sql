/****** Object:  UserDefinedFunction [dbo].[fn_HasCandidateNotScheduled]    Script Date: 8/27/2021 7:48:59 AM ******/
IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_HasCandidateNotScheduled]')
                      AND type IN ( N'FN' ))
BEGIN
    DROP FUNCTION dbo.fn_HasCandidateNotScheduled
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create Function [dbo].[fn_HasCandidateNotScheduled](@candidateEmail varchar(150), @irfId varchar(100), @startDate datetime)
Returns bit
as
Begin

declare @isNotScheduled int
set @isNotScheduled=0

if not exists(select id from InterviewMeetings where candidateEmail =@candidateEmail and IRFId =@irfId and confirmationStatus=1 and interviewDate >= @startDate )
begin
	set @isNotScheduled=1
end
 return @isNotScheduled

End
GO


