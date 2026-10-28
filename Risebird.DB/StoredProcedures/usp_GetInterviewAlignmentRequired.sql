IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviewAlignmentRequired'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewAlignmentRequired
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- The interview alignment rollout switch for one interview, read from that interview's own channel.
--
-- Every alignment endpoint (trial acknowledgement, join-gate override, code-session questions and
-- minting, the inline insights, and the candidate's code-session page) calls this alongside
-- IsInterviewerForInterview. A page gate does not protect a route, and the flag must never come from
-- anything the caller supplies - so it is resolved here from InterviewMeetings -> IRF, nowhere else.
-- NULL (every channel on day one) and 0 both mean off.
CREATE PROCEDURE [dbo].[usp_GetInterviewAlignmentRequired]
    @InterviewId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CAST(ISNULL(MAX(CAST(irf.IsInterviewerAlignmentRequired AS INT)), 0) AS BIT) AS IsInterviewerAlignmentRequired
    FROM dbo.InterviewMeetings im
    INNER JOIN dbo.IRF irf ON irf.IRFId = im.IRFId
    WHERE im.id = @InterviewId;
END
GO
