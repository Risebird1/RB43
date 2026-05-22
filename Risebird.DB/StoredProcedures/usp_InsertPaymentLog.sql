IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InsertPaymentLog'))
BEGIN
    DROP PROCEDURE dbo.usp_InsertPaymentLog
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ============================================================
-- Procedure : usp_InsertPaymentLog
-- Purpose   : Inserts one row into PaymentLogs capturing the
--             full context of a single TransactionStatus webhook
--             call: raw request, response, parsed fields, HTTP
--             status, and any error message.
-- Returns   : Newly inserted Id.
-- ============================================================
CREATE PROCEDURE usp_InsertPaymentLog
    @Endpoint           NVARCHAR(200)  = NULL,
    @RequestBody        NVARCHAR(MAX)  = NULL,
    @ResponseBody       NVARCHAR(MAX)  = NULL,
    @HttpStatusCode     INT            = NULL,
    @LogLevel           NVARCHAR(50)   = NULL,
    @EventType          NVARCHAR(100)  = NULL,
    @TransferId         NVARCHAR(50)   = NULL,
    @CfTransferId       NVARCHAR(100)  = NULL,
    @TransferStatus     NVARCHAR(50)   = NULL,
    @SignatureTimestamp NVARCHAR(50)   = NULL,
    @SignatureReceived  NVARCHAR(500)  = NULL,
    @RowsUpdated        INT            = NULL,
    @ErrorMessage       NVARCHAR(MAX)  = NULL,
    @CallerIp           NVARCHAR(50)   = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[PaymentLogs]
    (
        Endpoint, RequestBody, ResponseBody, HttpStatusCode,
        LogLevel, EventType, TransferId, CfTransferId,
        TransferStatus, SignatureTimestamp, SignatureReceived,
        RowsUpdated, ErrorMessage, CallerIp, CreatedAt
    )
    VALUES
    (
        @Endpoint, @RequestBody, @ResponseBody, @HttpStatusCode,
        @LogLevel, @EventType, @TransferId, @CfTransferId,
        @TransferStatus, @SignatureTimestamp, @SignatureReceived,
        @RowsUpdated, @ErrorMessage, @CallerIp, GETUTCDATE()
    );

    SELECT SCOPE_IDENTITY() AS InsertedId;
END
GO
