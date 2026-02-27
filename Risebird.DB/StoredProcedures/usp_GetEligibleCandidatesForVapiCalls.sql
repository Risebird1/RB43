-- =============================================
-- Author: Vapi Integration
-- Create date: 2026-02-24
-- Updated:     2026-02-25
-- Description: Gets eligible candidates for automated Vapi interview scheduling calls.
--
-- Returns candidates who:
--   1. Have a valid phone number and email
--   2. Have NOT yet had a confirmed interview booked (InterviewMeetings.confirmationStatus != '1')
--   3. Have fewer than 2 Vapi call attempts (tracked directly on Candidate.VapiCallAttempts)
--
-- Call attempt tracking:
--   Candidate.VapiCallAttempts is incremented by usp_IncrementCandidateVapiCallAttempts
--   immediately after each successful Vapi call is initiated. This column is the
--   single source of truth for attempt counts; VapiCallLogs is not consulted here.
--
-- Schema notes:
--   Candidate.id (INT)          = primary key
--   Candidate.IRFId (INT)       → references IRF.id
--   IRF.id (INT)                = primary key
--   IRF.IRFId (VARCHAR)         = business identifier
--   IRF.ClientId (INT)          → references Client.id
--   Candidate.CandidateMasterId → CandidatesMaster.id → UserList.id → EmailId (email)
--   InterviewMeetings.IRFId     = IRF.IRFId (VARCHAR match)
-- =============================================

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetEligibleCandidatesForVapiCalls'))
BEGIN
    DROP PROCEDURE dbo.usp_GetEligibleCandidatesForVapiCalls
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetEligibleCandidatesForVapiCalls]
    @Count INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Count)
        c.id                                    AS CandidateId,
        c.IRFId,
        irf.IRFId                               AS IRFName,
        (u.FirstName + ' ' + u.LastName)        AS CandidateName,
        u.EmailId                               AS CandidateEmail,
        c.Phone                                 AS CandidatePhone,
        irf.IRFId                               AS JobTitle,
        cl.Name                                 AS ClientName,
        c.VapiCallAttempts                      AS CallAttempts
    FROM Candidate c
    INNER JOIN IRF             irf ON irf.id        = c.IRFId
    INNER JOIN Client          cl  ON cl.id         = irf.ClientId
    INNER JOIN CandidatesMaster cm ON cm.id         = c.CandidateMasterId
    INNER JOIN UserList         u  ON u.id          = cm.UserId
    WHERE
        -- Must have a valid email
        u.EmailId IS NOT NULL
        -- Must have a valid phone number
        AND c.Phone IS NOT NULL
        AND LEN(c.Phone) > 0
        -- Fewer than 2 Vapi call attempts have been made for this candidate+IRF
        AND c.VapiCallAttempts < 2
        -- No confirmed interview meeting already exists for this candidate+IRF
        AND NOT EXISTS (
            SELECT 1
            FROM InterviewMeetings im
            WHERE im.candidateEmail    = u.EmailId
              AND im.IRFId             = irf.IRFId
              AND im.confirmationStatus = '1'
        )
    ORDER BY
        -- Candidates never called first, then those called once
        c.VapiCallAttempts ASC,
        -- Within the same attempt bucket, prefer more recently added candidates
        c.id DESC;
END
GO
