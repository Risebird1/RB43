IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllIRFLegacyList'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllIRFLegacyList
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetAllIRFLegacyList]
    @emailid      VARCHAR(200),
    @profileName  VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF @emailid IS NOT NULL AND @emailid <> '' AND @profileName = 'Client'
    BEGIN
        SELECT IRF.*, Client.[Name] AS ClientCompName
        FROM dbo.IRF
        INNER JOIN dbo.Client ON IRF.ClientId = Client.id
        INNER JOIN dbo.TransClientUser tc ON tc.IRFId = IRF.id
        INNER JOIN dbo.UserList ul ON ul.id = tc.ClientUserId
        WHERE ul.EmailId = @emailid
        ORDER BY IRF.EndDate DESC;
        RETURN;
    END

    IF @emailid IS NOT NULL AND @emailid <> '' AND @profileName = 'Recruiter'
    BEGIN
        SELECT IRF.*, Client.[Name] AS ClientCompName
        FROM dbo.IRF
        INNER JOIN dbo.Client ON IRF.ClientId = Client.id
        INNER JOIN dbo.TransRecruiter tr ON tr.IRFId = IRF.id
        INNER JOIN dbo.UserList ul ON ul.id = tr.UserId
        WHERE ul.EmailId = @emailid
        ORDER BY IRF.EndDate DESC;
        RETURN;
    END

    SELECT IRF.*, Client.[Name] AS ClientCompName
    FROM dbo.IRF
    INNER JOIN dbo.Client ON IRF.ClientId = Client.id
    ORDER BY IRF.EndDate DESC;
END
GO
