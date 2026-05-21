IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClientBillingOwners'))
    DROP PROCEDURE dbo.usp_GetClientBillingOwners
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns all distinct billing owners (from active IRFs) for a given client.
CREATE PROCEDURE usp_GetClientBillingOwners
    @ClientId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DISTINCT
        ul.id        AS BillingOwnerId,
        ul.EmailId   AS BillingOwnerEmail
    FROM IRF
    JOIN UserList ul ON ul.id = IRF.BillingOwner
    WHERE IRF.ClientId  = @ClientId
      AND IRF.BillingOwner IS NOT NULL
    ORDER BY ul.EmailId
END
GO
