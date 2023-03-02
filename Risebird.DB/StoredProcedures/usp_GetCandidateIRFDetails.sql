IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateIRFDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateIRFDetails
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_GetCandidateIRFDetails
@IRFId varchar(15),
@uniqueid varchar(50)
AS
BEGIN
Declare @CandidateEmail varchar(200)
Select @CandidateEmail=CandidateEmail From ScheduleInterviewEvent where uniqueid=@uniqueid
Select CandidateName,candidateEmail,resumeLink,staffingAgency,ClientHrContact,MobileNumber from InterviewMeetings where IRFId=@IRFId and candidateEmail=@CandidateEmail
END
GO