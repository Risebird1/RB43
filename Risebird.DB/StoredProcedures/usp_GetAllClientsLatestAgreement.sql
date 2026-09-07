IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetAllClientsLatestAgreement'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllClientsLatestAgreement
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns exactly one row per client — the most recently added agreement.
CREATE PROCEDURE usp_GetAllClientsLatestAgreement
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Ranked AS (
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
            ca.ModifiedOn,
            ROW_NUMBER() OVER (PARTITION BY ca.ClientId ORDER BY ca.CreatedOn DESC, ca.id DESC) AS rn
        FROM ClientAgreement ca
    )
    SELECT id, ClientId, AgreementType, StartDate, ExpireDate, Status, AgreementFileUrl, Remarks,
           CreatedBy, CreatedOn, ModifiedBy, ModifiedOn
    FROM Ranked
    WHERE rn = 1;
END
GO
