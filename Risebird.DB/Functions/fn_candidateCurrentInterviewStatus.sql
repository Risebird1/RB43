
IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_candidateCurrentInterviewStatus]')
                      AND type IN ( N'FN' ))
BEGIN
    DROP FUNCTION dbo.fn_candidateCurrentInterviewStatus
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Function fn_candidateCurrentInterviewStatus(@emailId varchar(100), @irfName varchar(25))
Returns varchar(50)
as
Begin

	declare @currentInterviewStatus int, @currentInterviewStatusDesc varchar(50) = 'NA'

	set @currentInterviewStatus = (select top 1 ic.InterviewStatus from InterviewMeetings im
	join InterviewComments ic on im.id = ic.InterviewId where im.confirmationStatus=1 and candidateEmail = @emailId and irfid = @irfName order by interviewDate desc)


	if(@currentInterviewStatus is null)
	Begin
		return @currentInterviewStatusDesc
	End
	if(@currentInterviewStatus = 1)
	Begin
		set @currentInterviewStatusDesc = 'Proxy'
	End
	else if(@currentInterviewStatus = 2)
	Begin
		set @currentInterviewStatusDesc = 'Strongly Selected'
	End
	else if(@currentInterviewStatus = 3)
	Begin
		set @currentInterviewStatusDesc = 'Strongly Rejected'
	End
	else if(@currentInterviewStatus = 4)
	Begin
		set @currentInterviewStatusDesc = 'Marginally Selected'
	End
	else if(@currentInterviewStatus = 5)
	Begin
		set @currentInterviewStatusDesc = 'Marginally Rejected'
	End

	return @currentInterviewStatusDesc


end





