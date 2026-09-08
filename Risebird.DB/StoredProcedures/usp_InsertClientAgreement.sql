IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_InsertClientAgreement'))
BEGIN
    DROP PROCEDURE dbo.usp_InsertClientAgreement
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_InsertClientAgreement
    @ClientId          INT,
    @AgreementType     INT,
    @StartDate         DATETIME = NULL,
    @ExpireDate        DATETIME = NULL,
    @Status            INT = 1,   -- 1=Active, 2=Terminated
    @AgreementFileUrl  VARCHAR(500) = NULL,
    @Remarks           VARCHAR(500) = NULL,
    @CreatedBy         VARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Now DATETIME = DATEADD(MINUTE, 330, GETDATE());

    INSERT INTO ClientAgreement (
        ClientId, AgreementType, StartDate, ExpireDate, Status,
        AgreementFileUrl, Remarks, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn
    )
    VALUES (
        @ClientId, @AgreementType, @StartDate, @ExpireDate, @Status,
        @AgreementFileUrl, @Remarks, @CreatedBy, @Now, @CreatedBy, @Now
    );

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewClientAgreementId;
END
GO
