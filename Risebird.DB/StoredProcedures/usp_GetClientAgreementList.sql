IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetClientAgreementList'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientAgreementList
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_GetClientAgreementList
    @ClientId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ca.id,
        ca.ClientId,
        ca.AgreementType,
        ca.StartDate,
        ca.ExpireDate,
        ca.Status,
        ca.AgreementFileUrl,
        ca.Remarks,
        ca.CreatedBy,
        ca.CreatedOn,
        ca.ModifiedBy,
        ca.ModifiedOn
    FROM ClientAgreement ca
    WHERE ca.ClientId = @ClientId
    ORDER BY ca.CreatedOn DESC;
END
GO
