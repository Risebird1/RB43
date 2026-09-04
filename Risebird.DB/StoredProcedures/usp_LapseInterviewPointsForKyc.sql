IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_LapseInterviewPointsForKyc'))
BEGIN
    DROP PROCEDURE dbo.usp_LapseInterviewPointsForKyc
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Flips all still-unpaid InterviewPoints for the given interviewer/month to 'Lapsed'
-- because KYC was not completed within the tracking window. Month is matched off
-- InterviewMeetings.interviewDate for Pending rows (falling back to InterviewPoints.Date
-- if there's no linked meeting), and off InterviewPoints.Date directly for Submitted rows
-- -- matching how usp_FetchKycIncompletePayoutCandidates computed the tracked month.
CREATE PROCEDURE [dbo].[usp_LapseInterviewPointsForKyc]
    @Userid INT,
    @Month  VARCHAR(7)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE ip
    SET ip.Status = 'Lapsed'
    FROM InterviewPoints ip
    LEFT JOIN InterviewMeetings im ON im.id = ip.Interviewid
    WHERE ip.userid = @Userid
      AND (
            (ip.Status = 'Pending'   AND FORMAT(ISNULL(im.interviewDate, ip.[Date]), 'yyyy-MM') = @Month)
         OR (ip.Status = 'Submitted' AND FORMAT(ip.[Date], 'yyyy-MM') = @Month)
      );
END
GO
