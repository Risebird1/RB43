IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_CheckFutureCancelInterview'))
BEGIN
    DROP PROCEDURE dbo.usp_CheckFutureCancelInterview
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_CheckFutureCancelInterview
@interviewdate datetime,
@CandidateEmailid varchar(150),
@IRFId varchar(15)
AS
BEGIN
Declare @Interviewid int=0
	Select @Interviewid=ISNULL(id,0) from InterviewMeetings where interviewdate>@interviewdate and candidateEmail=@CandidateEmailid and IRFid=@IRFId and ConfirmationStatus=2
	--Update Flag in interview Table to not consider the interview
	if(@Interviewid>0)
	begin
		Update InterviewMeetings set IsDeleted=1 where id=@Interviewid
	end
END
GO