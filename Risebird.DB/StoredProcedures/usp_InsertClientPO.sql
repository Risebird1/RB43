IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_InsertClientPO'))
BEGIN
    DROP PROCEDURE dbo.usp_InsertClientPO
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_InsertClientPO
    @ClientId        INT,
    @BillingType     INT,
    @BillingOwnerId  INT = NULL,
    @BillingModel    INT,
    @PONumber        VARCHAR(200) = NULL,
    @POCreditAmount  DECIMAL(18, 2),
    @POMonth         INT,
    @POYear          INT,
    @PODocumentUrl   VARCHAR(500) = NULL,
    @CreatedBy       VARCHAR(200) = NULL,
    @Remarks         VARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Now    DATETIME = DATEADD(MINUTE, 330, GETDATE());
    DECLARE @Status INT = CASE
        WHEN @POCreditAmount > 0 THEN 0   -- AwaitingCash
        ELSE 3                            -- DocumentOnly
    END;

    INSERT INTO ClientPO (
        ClientId, BillingType, BillingOwnerId, BillingModel,
        PONumber, POCreditAmount, CashCreditReceived,
        POMonth, POYear, PODocumentUrl, Status,
        Remarks, CreatedBy, CreatedOn, ModifiedOn
    )
    VALUES (
        @ClientId, @BillingType, @BillingOwnerId, @BillingModel,
        @PONumber, @POCreditAmount, 0,
        @POMonth, @POYear, @PODocumentUrl, @Status,
        @Remarks, @CreatedBy, @Now, @Now
    );

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewClientPOId;
END
GO
