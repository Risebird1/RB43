-- =============================================
-- Author: Vapi Integration
-- Create date: 2026-02-24
-- Description: Gets eligible candidates for automated Vapi interview scheduling calls
-- Returns candidates who:
--   1. Don't have a confirmed interview (InterviewMeetings.comfirmationStatus != 1) for their IRF
--   2. Haven't been called more than 2 times per IRFId (tracked in VapiCallLogs per email+IRFId)
--   3. Have valid email and phone number
-- Note: Call attempts are tracked PER candidate PER IRFId combination
--       Same email can be called for multiple IRFs independently
-- 
-- IMPORTANT Schema Note:
--   - IRF.id (INT) = Primary Key
--   - IRF.IRFId (VARCHAR) = Business identifier
--   - Candidate.IRFId (INT) → References IRF.id
--   - Candidate.CandidateMasterId (INT) → References CandidatesMaster.id
--   - CandidatesMaster.UserId (INT) → References UserList.id
--   - UserList.EmailId (VARCHAR) = Source of candidate email (ONLY source!)
--   - VapiCallLogs.IRFId (INT) → References IRF.id
--   - InterviewMeetings.IRFId (VARCHAR) → References IRF.IRFId
-- Three-way relationship for email:
--   Candidate → CandidatesMaster → UserList → EmailId
--   c.CandidateMasterId = cm.id
--   cm.UserId = u.id
--   u.EmailId (final source)
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

    -- CTE to count previous call attempts per candidate email AND IRFId combination
    -- A candidate can be in multiple IRFs, so we track attempts per IRF separately
    WITH CandidateCallCounts AS (
        SELECT 
            RecipientEmail,
            IRFId,
            COUNT(*) AS CallAttempts,
            MAX(CreatedAt) AS LastCallAttempt
        FROM VapiCallLogs
        WHERE RecipientEmail IS NOT NULL
            AND IRFId IS NOT NULL
        GROUP BY RecipientEmail, IRFId
    )
    
    -- Main query to fetch eligible candidates
    SELECT TOP (@Count)
        c.id AS CandidateId,
        c.IRFId,
        irf.IRFId AS IRFName,
        c.Name AS CandidateName,
        u.EmailId AS CandidateEmail,
        c.Phone AS CandidatePhone,
        irf.IRFId AS JobTitle,
        ISNULL(ccc.CallAttempts, 0) AS CallAttempts,
        ccc.LastCallAttempt
    FROM Candidate c
    INNER JOIN IRF irf ON c.IRFId = irf.id
    INNER JOIN CandidatesMaster cm ON c.CandidateMasterId = cm.id
    INNER JOIN UserList u ON cm.UserId = u.id
    LEFT JOIN CandidateCallCounts ccc ON u.EmailId = ccc.RecipientEmail 
        AND c.IRFId = ccc.IRFId
    WHERE 
        -- Must have valid email (from UserList only)
        u.EmailId IS NOT NULL
        -- Must have valid phone number
        AND c.Phone IS NOT NULL
        AND LEN(c.Phone) > 0
        -- No confirmed interview meeting for this IRFId and candidate email combination
        -- Note: InterviewMeetings.IRFId matches with IRF.IRFId (both VARCHAR)
        AND NOT EXISTS (
            SELECT 1 
            FROM InterviewMeetings im
            WHERE im.candidateEmail = u.EmailId
            AND im.IRFId = irf.IRFId
            AND im.comfirmationStatus = '1'
        )
        -- Maximum 2 call attempts OR no successful booking yet
        AND (
            -- Either called less than 2 times
            ISNULL(ccc.CallAttempts, 0) < 2
            -- OR called but never successfully booked an interview for this IRF (eligible for retry)
            OR NOT EXISTS (
                SELECT 1 
                FROM VapiCallLogs vcl
                WHERE vcl.RecipientEmail = u.EmailId
                AND vcl.IRFId = c.IRFId
                AND vcl.InterviewBooked = 1
            )
        )
    ORDER BY 
        -- Prioritize candidates with fewer call attempts (0 attempts first, then 1 attempt)
        ISNULL(ccc.CallAttempts, 0) ASC,
        -- Among same attempt counts, prioritize most recent candidates
        c.id DESC;
END
GO
