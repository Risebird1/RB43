
IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_IsCandidateSelected]')
                      AND type IN ( N'FN' ))
BEGIN
    DROP FUNCTION dbo.fn_IsCandidateSelected
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Function fn_IsCandidateSelected(@candidateEmail varchar(150), @irfId varchar(100), @startDate datetime)
Returns bit
as
Begin

declare @isSelected bit
set @isSelected=0

if exists(select im.id from InterviewMeetings im join InterviewComments ic on ic.InterviewId = im.id left join QCFeedbackReport qcf on qcf.InterviewId = im.id    
 where im.candidateEmail=@candidateEmail and im.IRFId =@irfId and im.confirmationStatus=1 and (ic.InterviewStatus= 2 or ic.InterviewStatus=4) and qcf.InterviewId is not null and im.interviewDate >= @startDate)
begin
	set @isSelected=1
end
 return @isSelected

End