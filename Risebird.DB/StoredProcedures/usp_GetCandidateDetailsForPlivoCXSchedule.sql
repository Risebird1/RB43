IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetCandidateDetailsForPlivoCXSchedule'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateDetailsForPlivoCXSchedule
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns the candidate and IRF details required to call the Schedule API
-- for a Plivo CX outbound interview scheduling call.
-- Called from PlivoCXCallRepository.GetCandidateDetailsForScheduleAsync.

CREATE PROCEDURE dbo.usp_GetCandidateDetailsForPlivoCXSchedule
    @IRFId          INT,
    @CandidateEmail VARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ca.ResumeLink,
        ca.Phone,
        IRF.IRFId,
        cl.tenantId,
        ca.ClientHRAttemptContact,
        LTRIM(RTRIM(ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, ''))) AS CandidateName,
        ISNULL(sc.duration, 60) AS duration
    FROM   Candidate              ca
    JOIN   CandidatesMaster       cm  ON cm.id         = ca.CandidateMasterId
    JOIN   UserList               ul  ON ul.id         = cm.UserId
    JOIN   IRF                        ON IRF.id        = ca.IRFId
    JOIN   Client                 cl  ON cl.id         = IRF.ClientId
    LEFT JOIN ScheduleInterviewEvent sc ON sc.SkillChannel = IRF.IRFId
    WHERE  ca.IRFId    = @IRFId
      AND  ul.EmailId  = @CandidateEmail;
END
GO
