IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateClientAgreement'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateClientAgreement
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_UpdateClientAgreement
    @AgreementId       INT,
    @ClientId          INT,
    @AgreementType     INT,
    @StartDate         DATETIME = NULL,
    @ExpireDate        DATETIME = NULL,
    @Status            INT,
    @Remarks           VARCHAR(500) = NULL,
    @ModifiedBy        VARCHAR(200) = NULL,
    @AgreementFileUrl  VARCHAR(500) = NULL,
    @UpdateDocument     BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Now DATETIME = DATEADD(MINUTE, 330, GETDATE());
    DECLARE @CurrentDocUrl VARCHAR(500);

    SELECT @CurrentDocUrl = AgreementFileUrl
    FROM ClientAgreement
    WHERE id = @AgreementId AND ClientId = @ClientId;

    IF @@ROWCOUNT = 0
    BEGIN
        SELECT CAST(0 AS BIT) AS Success;
        RETURN;
    END

    UPDATE ClientAgreement
    SET
        AgreementType    = @AgreementType,
        StartDate        = @StartDate,
        ExpireDate       = @ExpireDate,
        Status           = @Status,
        Remarks          = @Remarks,
        AgreementFileUrl = CASE WHEN @UpdateDocument = 1 THEN @AgreementFileUrl ELSE @CurrentDocUrl END,
        ModifiedBy       = @ModifiedBy,
        ModifiedOn       = @Now
    WHERE id = @AgreementId AND ClientId = @ClientId;

    SELECT CAST(CASE WHEN @@ROWCOUNT > 0 THEN 1 ELSE 0 END AS BIT) AS Success;
END
GO
