IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllRevenueDetailsByDate'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllRevenueDetailsByDate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Revenue export (Excel) — per-IRF rows.
-- BaseAmount uses the same billing-type branching as usp_GetRevenueDetailsByDate.
-- Turing (id=177): MeetingStatus 1/3/9 included at 30%.
CREATE PROCEDURE usp_GetAllRevenueDetailsByDate
    @StartDate DATETIME,
    @EndDate   DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        cl.id,
        cl.Name,
        COUNT(im.id)                                                                      AS TotalCount,
        SUM(CASE WHEN ISNULL(IRF.IsAIInterview, 0) = 0 THEN 1 ELSE 0 END)               AS NonAIInterviewCount,
        SUM(CASE WHEN ISNULL(IRF.IsAIInterview, 0) = 1 THEN 1 ELSE 0 END)               AS AIInterviewCount,
        cl.PerInterviewRate,
        ISNULL(cl.AIPerInterviewRate, 0)                                                  AS AIPerInterviewRate,
        ISNULL(cl.InterviewerBillingType, 1)                                              AS InterviewerBillingType,
        ISNULL(cl.BillingCurrency, 'INR')                                                 AS BillingCurrency,
        IRF.IRFId,
        ul.EmailId                                                                        AS BillingOwner,
        SUM(
            CASE
                WHEN ISNULL(cl.InterviewerBillingType, 1) = 2
                    THEN CAST(ISNULL(scbr.BillingRate, cl.PerInterviewRate) AS DECIMAL(18,2))
                WHEN ISNULL(cl.InterviewerBillingType, 1) = 3
                    THEN CAST(ISNULL(ibr.BillingRate, 0) AS DECIMAL(18,2))
                ELSE
                    CASE WHEN ISNULL(IRF.IsAIInterview, 0) = 1
                         THEN CAST(ISNULL(cl.AIPerInterviewRate, 0) AS DECIMAL(18,2))
                         ELSE CAST(ISNULL(cl.PerInterviewRate,   0) AS DECIMAL(18,2))
                    END
            END
            * CASE WHEN cl.id = 177 AND ic.MeetingStatus IN (1, 3, 9)
                   THEN CAST(0.30 AS DECIMAL(5,2))
                   ELSE CAST(1.00 AS DECIMAL(5,2))
              END
        )                                                                                  AS BaseAmount
    FROM InterviewMeetings im
    JOIN IRF               ON IRF.IRFId       = im.IRFId
    JOIN InterviewComments ic ON ic.InterviewId = im.id
    JOIN Client cl         ON IRF.ClientId    = cl.id
    LEFT JOIN UserList ul  ON IRF.BillingOwner = ul.id
    OUTER APPLY (
        SELECT TOP 1 BillingRate
        FROM SkillChannelBillingRate
        WHERE IRFId = IRF.id
    ) scbr
    OUTER APPLY (
        SELECT TOP 1 ul_int.id AS UserId
        FROM UserList ul_int
        WHERE ul_int.EmailId = im.interviewer
    ) int_user
    OUTER APPLY (
        SELECT TOP 1 BillingRate
        FROM InterviewerBillingRate
        WHERE IRFId  = IRF.id
          AND Userid = ISNULL(int_user.UserId, 0)
    ) ibr
    WHERE im.interviewDate >= @StartDate
      AND im.interviewDate <  @EndDate
      AND (
            ic.MeetingStatus IN (4, 6, 7, 8)
            OR (cl.id = 177 AND ic.MeetingStatus IN (1, 3, 9))
          )
      AND ISNULL(im.RedoInterview, 0) = 0
    GROUP BY
        cl.id, cl.Name, cl.PerInterviewRate, cl.AIPerInterviewRate,
        cl.InterviewerBillingType, cl.BillingCurrency,
        IRF.IRFId, ul.EmailId
END
GO
