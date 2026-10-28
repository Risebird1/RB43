IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveJoinGateOverride'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveJoinGateOverride
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Records an interviewer choosing "join without confirming" on the interview alignment page, with the
-- briefing points that were still outstanding. The caller has already verified the interviewer owns
-- the interview (IsInterviewerForInterview).
CREATE PROCEDURE [dbo].[usp_SaveJoinGateOverride]
    @InterviewId             INT,
    @InterviewerEmail        VARCHAR(200),
    @OutstandingRequirements VARCHAR(500),
    @CurrentDate             DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.InterviewJoinGateOverrides (InterviewId, InterviewerEmail, OutstandingRequirements, CreatedOn)
    VALUES (@InterviewId, LTRIM(RTRIM(@InterviewerEmail)), @OutstandingRequirements, @CurrentDate);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
END
GO
