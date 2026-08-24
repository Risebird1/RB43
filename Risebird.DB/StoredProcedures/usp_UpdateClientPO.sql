IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateClientPO'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateClientPO
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_UpdateClientPO
    @POId            INT,
    @ClientId        INT,
    @PONumber        VARCHAR(200) = NULL,
    @POCreditAmount  DECIMAL(18, 2),
    @POMonth         INT,
    @POYear          INT,
    @POQuantity      INT = NULL,
    @AddedDate       DATETIME = NULL,
    @ExpireDate      DATETIME = NULL,
    @PODocumentUrl   VARCHAR(500) = NULL,
    @UpdateDocument  BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Now DATETIME = DATEADD(MINUTE, 330, GETDATE());
    DECLARE @CashReceived DECIMAL(18, 2);
    DECLARE @CurrentStatus INT;
    DECLARE @CurrentDocUrl VARCHAR(500);

    SELECT
        @CashReceived  = CashCreditReceived,
        @CurrentStatus = Status,
        @CurrentDocUrl = PODocumentUrl
    FROM ClientPO
    WHERE id = @POId AND ClientId = @ClientId;

    IF @CashReceived IS NULL
    BEGIN
        SELECT CAST(0 AS BIT) AS Success;
        RETURN;
    END

    IF @POCreditAmount < @CashReceived
    BEGIN
        SELECT CAST(0 AS BIT) AS Success;
        RETURN;
    END

    -- Keep Complete/Paid statuses; otherwise recompute from credit vs cash
    DECLARE @NewStatus INT = @CurrentStatus;
    IF @CurrentStatus < 4
    BEGIN
        IF @POCreditAmount <= 0
            SET @NewStatus = 3; -- DocumentOnly
        ELSE IF @CashReceived <= 0
            SET @NewStatus = 0; -- AwaitingCash
        ELSE IF @CashReceived < @POCreditAmount
            SET @NewStatus = 1; -- PartialCash
        ELSE
            SET @NewStatus = 2; -- CashCredited
    END

    UPDATE ClientPO
    SET
        PONumber       = @PONumber,
        POCreditAmount = @POCreditAmount,
        POMonth        = @POMonth,
        POYear         = @POYear,
        POQuantity     = @POQuantity,
        AddedDate      = @AddedDate,
        ExpireDate     = @ExpireDate,
        PODocumentUrl  = CASE WHEN @UpdateDocument = 1 THEN @PODocumentUrl ELSE @CurrentDocUrl END,
        Status         = @NewStatus,
        ModifiedOn     = @Now
    WHERE id = @POId AND ClientId = @ClientId;

    SELECT CAST(CASE WHEN @@ROWCOUNT > 0 THEN 1 ELSE 0 END AS BIT) AS Success;
END
GO
