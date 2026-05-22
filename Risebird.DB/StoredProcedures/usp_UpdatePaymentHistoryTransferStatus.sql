IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdatePaymentHistoryTransferStatus'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdatePaymentHistoryTransferStatus
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- ============================================================
-- Procedure : usp_UpdatePaymentHistoryTransferStatus
-- Purpose   : Updates PayoutStatus, TransferUtr (and back-fills
--             CfTransferId) on a PaymentHistory row identified by
--             TransferId and/or CfTransferId received from a
--             Cashfree Payout webhook event.
--
-- Match logic:
--   A row is matched when EITHER:
--     • TransferId   = @TransferId   (when @TransferId is supplied)
--     • CfTransferId = @CfTransferId (when @CfTransferId is supplied)
--
-- CfTransferId / TransferUtr back-fill:
--   Both fields are kept if already populated and the webhook
--   does not supply a new value.
--
-- Returns   : @@ROWCOUNT — number of rows updated (0 = no match).
-- ============================================================
CREATE PROCEDURE usp_UpdatePaymentHistoryTransferStatus
    @TransferId   NVARCHAR(50)  = NULL,
    @CfTransferId NVARCHAR(100) = NULL,
    @PayoutStatus NVARCHAR(50),
    @TransferUtr  VARCHAR(100)  = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE PaymentHistory
    SET
        PayoutStatus  = @PayoutStatus,
        CfTransferId  = CASE
                            WHEN @CfTransferId IS NOT NULL AND @CfTransferId <> ''
                            THEN @CfTransferId
                            ELSE CfTransferId
                        END,
        TransferUtr   = CASE
                            WHEN @TransferUtr IS NOT NULL AND @TransferUtr <> ''
                            THEN @TransferUtr
                            ELSE TransferUtr
                        END
    WHERE
        (
            (TransferId   = @TransferId   AND @TransferId   IS NOT NULL AND @TransferId   <> '')
            OR
            (CfTransferId = @CfTransferId AND @CfTransferId IS NOT NULL AND @CfTransferId <> '')
        );

    SELECT @@ROWCOUNT AS RowsUpdated;
END
GO
