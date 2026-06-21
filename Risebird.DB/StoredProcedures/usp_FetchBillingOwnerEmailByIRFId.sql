IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchBillingOwnerEmailByIRFId'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchBillingOwnerEmailByIRFId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_FetchBillingOwnerEmailByIRFId]
    @id INT
AS
BEGIN
    SELECT ul.EmailId
    FROM IRF
    JOIN UserList ul ON ul.id = IRF.BillingOwner
    WHERE IRF.id = @id
END
GO
