IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetPaymentDetailsByTransferId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetPaymentDetailsByTransferId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- ============================================================
-- Procedure : usp_GetPaymentDetailsByTransferId
-- Purpose   : Fetches the PaymentHistoryId, interviewer EmailId,
--             and Amount from PaymentHistory joined with UserList,
--             matched by TransferId and/or CfTransferId.
--             Used by the webhook to send a confirmation email
--             after a successful payout transfer.
--
-- Returns   : One row with PaymentHistoryId, Email, Amount.
-- ============================================================
CREATE PROCEDURE usp_GetPaymentDetailsByTransferId
    @TransferId   NVARCHAR(50)  = NULL,
    @CfTransferId NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        ph.id       AS PaymentHistoryId,
        ul.EmailId  AS Email,
        ph.Amount   AS Amount
    FROM PaymentHistory ph
    INNER JOIN UserList ul ON ul.id = ph.Userid
    WHERE
        (
            (ph.TransferId   = @TransferId   AND @TransferId   IS NOT NULL AND @TransferId   <> '')
            OR
            (ph.CfTransferId = @CfTransferId AND @CfTransferId IS NOT NULL AND @CfTransferId <> '')
        );
END
GO
