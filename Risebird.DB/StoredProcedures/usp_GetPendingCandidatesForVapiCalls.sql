-- =============================================
-- Author: Vapi Integration
-- Create date: 2026-01-29
-- Description: Gets candidates pending AI calls for interview booking
-- =============================================
CREATE PROCEDURE [dbo].[usp_GetPendingCandidatesForVapiCalls]
    @InterviewId INT = NULL,
    @DaysAhead INT = 7,
    @MaxResults INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    -- Get candidates who:
    -- 1. Have interviews scheduled in next N days
    -- 2. Haven't been called yet via Vapi
    -- 3. Haven't confirmed their interview
    -- 4. Have valid phone numbers
    
    SELECT TOP (@MaxResults)
        i.InterviewId,
        c.CandidateId,
        c.CandidateName,
        c.CandidateEmail,
        c.MobileNumber as CandidatePhone,
        jd.JDTitle as JobTitle,
        i.InterviewDate,
        i.InterviewStatus,
        CASE 
            WHEN vcl.VapiCallId IS NOT NULL THEN 1
            ELSE 0
        END as HasBeenCalled,
        vcl.CallStatus as LastCallStatus,
        vcl.InterviewBooked as LastCallBooked
    FROM Interviews i
    INNER JOIN Candidate c ON i.CandidateId = c.CandidateId
    INNER JOIN IRF irf ON i.IRFId = irf.IRFId
    INNER JOIN JDDetails jd ON irf.JDId = jd.JDId
    LEFT JOIN (
        -- Get most recent call for each candidate
        SELECT 
            CandidateId,
            VapiCallId,
            CallStatus,
            InterviewBooked,
            ROW_NUMBER() OVER (PARTITION BY CandidateId ORDER BY CreatedAt DESC) as RowNum
        FROM VapiCallLogs
    ) vcl ON c.CandidateId = vcl.CandidateId AND vcl.RowNum = 1
    WHERE 
        -- Filter by interview ID if provided
        (@InterviewId IS NULL OR i.InterviewId = @InterviewId)
        -- Interview is in the future
        AND i.InterviewDate BETWEEN GETDATE() AND DATEADD(day, @DaysAhead, GETDATE())
        -- Interview is not cancelled
        AND i.InterviewStatus NOT IN (4, 5) -- Assuming 4=Cancelled, 5=Rejected
        -- Candidate has phone number
        AND c.MobileNumber IS NOT NULL 
        AND LEN(c.MobileNumber) >= 10
        -- Either never called, or last call failed to book
        AND (vcl.VapiCallId IS NULL OR vcl.InterviewBooked IS NULL OR vcl.InterviewBooked = 0)
    ORDER BY i.InterviewDate ASC;
END
GO
