IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetClientPOList'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientPOList
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_GetClientPOList
    @ClientId       INT,
    @Month          INT = NULL,
    @Year           INT = NULL,
    @BillingOwnerId INT = NULL,
    @OpenOnly       BIT = 0   -- 1 = POs still awaiting full cash credit
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        cp.id,
        cp.ClientId,
        cp.BillingType,
        cp.BillingOwnerId,
        cp.BillingModel,
        cp.PONumber,
        cp.POCreditAmount,
        cp.CashCreditReceived,
        cp.POMonth,
        cp.POYear,
        cp.POQuantity,
        cp.AddedDate,
        cp.ExpireDate,
        cp.PODocumentUrl,
        cp.Status,
        cp.Remarks,
        cp.CreatedBy,
        cp.CreatedOn,
        cp.ModifiedOn,
        ISNULL(ul.EmailId, '') AS BillingOwnerEmail,
        RTRIM(LTRIM(ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, ''))) AS BillingOwnerName
    FROM ClientPO cp
    LEFT JOIN UserList ul ON ul.id = cp.BillingOwnerId
    WHERE cp.ClientId = @ClientId
      AND (@Month IS NULL OR cp.POMonth = @Month)
      AND (@Year IS NULL OR cp.POYear = @Year)
      AND (@BillingOwnerId IS NULL OR cp.BillingOwnerId = @BillingOwnerId)
      AND (
            @OpenOnly = 0
         OR (cp.POCreditAmount > 0 AND cp.CashCreditReceived < cp.POCreditAmount)
      )
    ORDER BY cp.POYear DESC, cp.POMonth DESC, cp.CreatedOn DESC;
END
GO
