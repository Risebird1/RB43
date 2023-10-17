IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchCandidateFeedbackByIRFId'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchCandidateFeedbackByIRFId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure usp_FetchCandidateFeedbackByIRFId  
@irfid varchar(20)
AS  
BEGIN  
 Select cfd.*,IRF.JDTiltle as PrimarySkill,im.Interviewer as interviewerEmail,im.CandidateEmail as EmailAddress,im.Interviewdate
 from CandidateFeedbackDetails cfd join InterviewMeetings im on cfd.Interviewid=im.id join IRF on im.IRFId=IRF.IRFId 
 join InterviewComments ic on ic.Interviewid=im.id 
 where im.Irfid=@irfid and ic.MeetingStatus=4
  
END 
GO