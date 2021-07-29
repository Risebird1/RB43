IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchCallRecording'))
BEGIN
    DROP PROCEDURE [dbo].[usp_FetchCallRecording]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_FetchCallRecording
@IRFId varchar(50),
@CandidateEmail varchar(150)
As
BEGIN
Select * From CallRecordingDetails where IRFId=@IRFId and CandidateEmail=@CandidateEmail
Union
Select cr.* from CallRecordingDetails cr join InterviewMeetings im on im.id=cr.Interviewid where im.IRFId=@IRFId and im.CandidateEmail=@CandidateEmail
END
Go


