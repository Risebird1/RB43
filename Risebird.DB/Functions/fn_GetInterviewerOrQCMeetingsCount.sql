
IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_GetInterviewerOrQCMeetingsCount]')
                      AND type IN ( N'FN' ))
BEGIN
    DROP FUNCTION dbo.fn_GetInterviewerOrQCMeetingsCount
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Function fn_GetInterviewerOrQCMeetingsCount(@IRFId varchar(50), @EmailId varchar(100), @Type varchar(5))
Returns int
as
Begin

declare @totalMeetings int
set @totalMeetings=0
 if(@Type='QC')
	 begin
	  set @totalMeetings = (select count(id) from InterviewMeetings where IRFId=@IRFId and QCInterviewer=@EmailId and (confirmationStatus=1 or confirmationStatus is null))
	 end
 else
	 begin
		set @totalMeetings = (select count(id) from InterviewMeetings where IRFId=@IRFId and interviewer=@EmailId and (confirmationStatus=1 or confirmationStatus is null))
	 end

 return @totalMeetings

End