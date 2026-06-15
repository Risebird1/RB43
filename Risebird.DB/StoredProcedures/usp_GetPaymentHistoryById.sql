IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetPaymentHistoryById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetPaymentHistoryById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_GetPaymentHistoryById
    @id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ph.id,
        ph.Userid,
        ul.EmailId          AS InterviewerEmailid,
        ul.FirstName,
        ph.PaymentDate      AS Date,
        ph.Status,
        ph.Amount           AS Points,
        ph.TotalAmount,
        ph.TransferId,
        ph.CfTransferId,
        ph.TransferUtr,
        ph.PayoutStatus,
        ph.TransactionDate,
        ph.Comment
    FROM  PaymentHistory ph
    JOIN  Userlist ul ON ul.id = ph.Userid
    WHERE ph.id = @id;
END
GO
