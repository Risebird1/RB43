IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchCandidateFeedbackByInterviewer'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchCandidateFeedbackByInterviewer
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_FetchCandidateFeedbackByInterviewer]
    @Emailid VARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        cfd.*,
        irf.JDTiltle AS PrimarySkill,
        im.Interviewer AS interviewerEmail,
        im.CandidateEmail AS EmailAddress,
        im.Interviewdate
    FROM dbo.InterviewMeetings im
    INNER JOIN dbo.CandidateFeedbackDetails cfd ON cfd.InterviewId = im.id
    INNER JOIN dbo.IRF irf ON irf.IRFId = im.IRFId
    WHERE im.confirmationStatus = '1'
      AND im.Interviewer = @Emailid;
END
GO
