IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetHoldPaymentInterviewers'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetHoldPaymentInterviewers]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetHoldPaymentInterviewers]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ul.id                                                           AS UserId,
        ip.Id                                                           AS id,
        ul.FirstName,
        ul.LastName,
        ul.EmailId,
        ul.MobileNumber,
        ip.NickName,
        ul.IsActive,
        ip.IsApproved,
        ip.IsHoldPayment,
        ip.HoldPaymentReason,
        ip.AssessmentGroupId,
        ip.CurrentRole,
        ip.Experience,
        ip.City,
        ip.Country,

        -- Primary skill (first mapped skill)
        (
            SELECT TOP 1 sk.[Name]
            FROM TransInterviewerSkill tis
            JOIN Skill sk ON sk.id = tis.SkillId
            WHERE tis.InterviewerProfileId = ip.Id
        )                                                               AS PrimarySkill,

        -- Number of interviews with payment pending (not yet paid out)
        ISNULL((
            SELECT COUNT(*)
            FROM InterviewPoints ipt
            WHERE ipt.userid = ul.id
              AND ipt.Status = 'Pending'
              AND ipt.Points > 0
        ), 0)                                                           AS PendingInterviewCount,

        -- Gross pending amount (sum of Points before any deductions)
        ISNULL((
            SELECT SUM(
                ipt.Points
                - (ipt.Points * ISNULL(ipt.PenaltyAmount, 0) / 100)
                + ISNULL(ipt.QCPoints, 0)
                - (ipt.Points * ISNULL(ipt.PaymentSLA, 0) / 100)
            )
            FROM InterviewPoints ipt
            WHERE ipt.userid = ul.id
              AND ipt.Status = 'Pending'
              AND ipt.Points > 0
        ), 0)                                                           AS TotalPendingAmount,

        -- Total already paid out (sum of all PaymentHistory amounts)
        ISNULL((
            SELECT SUM(ph.TotalAmount)
            FROM PaymentHistory ph
            WHERE ph.Userid = ul.id
        ), 0)                                                           AS TotalPaidAmount,

        -- Date of last payout
        (
            SELECT MAX(ph.PaymentDate)
            FROM PaymentHistory ph
            WHERE ph.Userid = ul.id
        )                                                               AS LastPaymentDate

    FROM InterviewerProfile ip
    JOIN UserList ul ON ul.id = ip.UserId
    WHERE ip.IsHoldPayment = 1
      AND ISNULL(ip.IsDeleted, 0) = 0
    ORDER BY TotalPendingAmount DESC, ul.FirstName, ul.LastName;
END
GO
