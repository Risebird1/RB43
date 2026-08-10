IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetSupportHREmails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSupportHREmails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetSupportHREmails]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT EmailId
    FROM Support
END
GO
