-- usp_GetIRFsForClient
-- Returns all active IRFs for a client (by sender email).
-- Used to populate the IRF dropdown in pending resume review modal.
CREATE OR ALTER PROCEDURE [dbo].[usp_GetIRFsForClient]
    @SenderEmail VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ClientId INT;
    
    -- Get client ID from sender email via ClientUser relationship
    SELECT TOP 1 @ClientId = cu.ClientId
    FROM dbo.UserList ul
    INNER JOIN dbo.ClientUser cu ON cu.UserId = ul.id
    WHERE ul.EmailId = @SenderEmail
      AND ul.IsActive = 1;
    
    IF @ClientId IS NULL
    BEGIN
        -- Try to find via email domain matching against CompanyLink
        DECLARE @Domain VARCHAR(150) = SUBSTRING(@SenderEmail, CHARINDEX('@', @SenderEmail) + 1, LEN(@SenderEmail));
        
        SELECT TOP 1 @ClientId = c.id
        FROM dbo.Client c
        WHERE c.CompanyLink LIKE '%' + @Domain + '%';
    END
    
    -- Return all IRFs for this client that are currently active
    SELECT
        irf.id              AS Id,
        irf.IRFId           AS IRFStringId,
        irf.JDTiltle        AS JDTitle,
        irf.SkillCluster    AS SkillCluster,
        irf.StartDate       AS StartDate,
        irf.EndDate         AS EndDate,
        irf.NoOfPosition    AS Positions,
        irf.RisebirdHr      AS RisebirdHrId,
        hrUser.EmailId      AS RisebirdHrEmail,
        LTRIM(RTRIM(ISNULL(hrUser.FirstName, '') + ' ' + ISNULL(hrUser.LastName, ''))) AS RisebirdHrName,
        c.Name              AS ClientName,
        c.id                AS ClientId
    FROM dbo.IRF irf
    INNER JOIN dbo.Client c ON c.id = irf.ClientId
    LEFT JOIN dbo.UserList hrUser ON hrUser.id = irf.RisebirdHr
    WHERE irf.ClientId = @ClientId
      AND irf.IsDeleted = 0
      AND GETDATE() BETWEEN ISNULL(irf.StartDate, '1900-01-01') AND ISNULL(irf.EndDate, '2100-01-01')
    ORDER BY irf.JDTiltle ASC;
END
GO
