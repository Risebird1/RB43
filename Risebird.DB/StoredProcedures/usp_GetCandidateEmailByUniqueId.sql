IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateEmailByUniqueId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateEmailByUniqueId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetCandidateEmailByUniqueId]
    @uniqueid VARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT EmailId, AadhaarVerified
    FROM UserList
    WHERE uniqueid = @uniqueid
END
GO
