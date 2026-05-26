IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdatePaymentHistoryGatewayStatus'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdatePaymentHistoryGatewayStatus
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ============================================================
-- Procedure : usp_UpdatePaymentHistoryGatewayStatus
-- Purpose   : Updates GatewayPaymentStatus and CfPaymentId on a
--             PaymentHistory row identified by CfPaymentId.
--             Called when a Cashfree Payment Gateway webhook
--             (PAYMENT_SUCCESS_WEBHOOK, PAYMENT_FAILED_WEBHOOK,
--              etc.) is received.
--
-- Match logic:
--   Matches the row where CfPaymentId = @CfPaymentId.
--
-- Returns   : @@ROWCOUNT — number of rows updated (0 = no match).
-- ============================================================
CREATE PROCEDURE usp_UpdatePaymentHistoryGatewayStatus
    @CfPaymentId          VARCHAR(100),
    @GatewayPaymentStatus NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE PaymentHistory
    SET
        GatewayPaymentStatus = @GatewayPaymentStatus
    WHERE
        CfPaymentId = @CfPaymentId
        AND @CfPaymentId IS NOT NULL
        AND @CfPaymentId <> '';

    SELECT @@ROWCOUNT AS RowsUpdated;
END
GO
