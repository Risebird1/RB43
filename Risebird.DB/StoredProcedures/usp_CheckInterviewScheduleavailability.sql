IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_CheckInterviewScheduleavailability'))
BEGIN
    DROP PROCEDURE dbo.usp_CheckInterviewScheduleavailability
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_CheckInterviewScheduleavailability
@candidateEmail varchar(200),
@IRFId varchar(15),
@timenow datetime
AS
BEGIN
select 'PastComplete' as interviewtype,interviewDate from [dbo].[InterviewMeetings] as intw join[dbo].[InterviewComments] as intc on intw.id = intc.interviewid 
where candidateEmail = @candidateEmail and (intc.MeetingStatus = 4 or intc.MeetingStatus = 7 or intc.MeetingStatus = 8) and IRFId= @IRFId and RedoInterview = 0
union 
select 'FutureScheduled' as interviewtype,interviewDate from [dbo].[InterviewMeetings] as intw join[dbo].[InterviewComments] as intc on intw.id = intc.interviewid 
where candidateEmail = @candidateEmail and IRFId= @IRFId and RedoInterview = 0
and interviewDate>=@timenow and confirmationStatus=1
union
Select 'OtherProfile' as interviewtype,'' as interviewDate From UserList where Emailid=@candidateEmail and idprofile!=23
END
GO