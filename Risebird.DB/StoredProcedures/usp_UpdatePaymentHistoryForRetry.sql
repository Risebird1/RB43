IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdatePaymentHistoryForRetry'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdatePaymentHistoryForRetry
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- ============================================================
-- Procedure : usp_UpdatePaymentHistoryForRetry
-- Purpose   : Resets a TRANSFER_FAILED PaymentHistory row so
--             it reflects the new retry payout attempt.
--             Matches the row by primary key (@id).
--             Clears TransferUtr and TransactionDate because
--             the new transfer has not yet settled.
--
-- Returns   : @@ROWCOUNT — number of rows updated (0 = no match).
-- ============================================================
CREATE PROCEDURE usp_UpdatePaymentHistoryForRetry
    @id           INT,
    @TransferId   NVARCHAR(50),
    @CfTransferId NVARCHAR(100) = NULL,
    @PayoutStatus NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE PaymentHistory
    SET
        TransferId      = @TransferId,
        CfTransferId    = @CfTransferId,
        PayoutStatus    = @PayoutStatus,
        TransferUtr     = NULL,
        TransactionDate = NULL,
        PaymentDate     = GETDATE()
    WHERE id = @id;

    SELECT @@ROWCOUNT AS RowsUpdated;
END
GO
