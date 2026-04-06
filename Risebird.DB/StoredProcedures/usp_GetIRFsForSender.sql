-- usp_GetIRFsForSender
-- Returns all active, non-paused IRFs for a given sender email.
-- TransClientUser.ClientUserId references UserList.id directly — no ClientUser
-- lookup is needed. Ordered by recency (EndDate → StartDate → CreatedOn) so
-- the C# scoring tiebreaker naturally favours the most recent channel.
--
-- SPECIAL CASE: If the sender is a Client Admin (RoleId = 9), they have access
-- to ALL IRFs for their client, even if not explicitly mapped in TransClientUser.
CREATE OR ALTER PROCEDURE [dbo].[usp_GetIRFsForSender]
    @SenderEmail VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserId   INT;
    DECLARE @ClientId INT;
    DECLARE @RoleId   INT;

    -- Step 1: Resolve UserList row for the sender
    SELECT TOP 1 @UserId = id
    FROM   dbo.UserList
    WHERE  LOWER(EmailId) = LOWER(@SenderEmail)
      AND  IsActive = 1;

    IF @UserId IS NULL
    BEGIN
        -- Sender not resolvable — return empty set
        SELECT TOP 0
            CAST(NULL AS INT)          AS Id,
            CAST(NULL AS VARCHAR(200)) AS IRFStringId,
            CAST(NULL AS VARCHAR(150)) AS JDTiltle,
            CAST(NULL AS VARCHAR(200)) AS SkillCluster,
            CAST(NULL AS INT)          AS RisebirdHrId,
            CAST(NULL AS VARCHAR(150)) AS RisebirdHrEmail,
            CAST(NULL AS DATETIME)     AS StartDate,
            CAST(NULL AS DATETIME)     AS EndDate,
            CAST(NULL AS DATETIME)     AS CreatedOn;
        RETURN;
    END

    -- Step 2: Check if user is a Client Admin (RoleId = 9)
    SELECT TOP 1 @ClientId = cu.ClientId, @RoleId = cu.RoleId
    FROM   dbo.ClientUser cu
    WHERE  cu.UserId = @UserId;

    -- If Client Admin (RoleId = 9), return ALL active IRFs for their client
    IF @RoleId = 9 AND @ClientId IS NOT NULL
    BEGIN
        SELECT DISTINCT
            irf.id                           AS Id,
            irf.IRFId                        AS IRFStringId,
            ISNULL(irf.JDTiltle,     '')     AS JDTiltle,
            ISNULL(irf.SkillCluster, '')     AS SkillCluster,
            irf.RisebirdHr                   AS RisebirdHrId,
            ISNULL(ul.EmailId,       '')     AS RisebirdHrEmail,
            irf.StartDate                    AS StartDate,
            irf.EndDate                      AS EndDate,
            irf.CreatedOn                    AS CreatedOn
        FROM   dbo.IRF irf
        INNER JOIN dbo.UserList ul ON ul.id = irf.RisebirdHr
        WHERE  irf.ClientId = @ClientId
          AND  irf.IsPaused = 0
          AND  ISNULL(irf.IsDeleted, 0) = 0
          AND  ul.IsActive  = 1
          AND  CAST(GETUTCDATE() AS DATE) >= CAST(irf.StartDate AS DATE)
          AND  CAST(GETUTCDATE() AS DATE) <= CAST(irf.EndDate   AS DATE)
        ORDER  BY irf.EndDate   DESC,
                  irf.StartDate DESC,
                  irf.CreatedOn DESC;
        RETURN;
    END

    -- Step 3: Regular users - check TransClientUser mappings
    SELECT DISTINCT
        irf.id                           AS Id,
        irf.IRFId                        AS IRFStringId,
        ISNULL(irf.JDTiltle,     '')     AS JDTiltle,
        ISNULL(irf.SkillCluster, '')     AS SkillCluster,
        irf.RisebirdHr                   AS RisebirdHrId,
        ISNULL(ul.EmailId,       '')     AS RisebirdHrEmail,
        irf.StartDate                    AS StartDate,
        irf.EndDate                      AS EndDate,
        irf.CreatedOn                    AS CreatedOn
    FROM   dbo.TransClientUser tcu
    INNER JOIN dbo.IRF         irf ON irf.id = tcu.IRFId
    INNER JOIN dbo.UserList    ul  ON ul.id  = irf.RisebirdHr
    WHERE  tcu.ClientUserId = @UserId   -- UserList.id
      AND  tcu.IsActive     = 1
      AND  irf.IsPaused     = 0
      AND  ul.IsActive      = 1
      AND  CAST(GETUTCDATE() AS DATE) >= CAST(irf.StartDate AS DATE)
      AND  CAST(GETUTCDATE() AS DATE) <= CAST(irf.EndDate   AS DATE)
    ORDER  BY irf.EndDate   DESC,
              irf.StartDate DESC,
              irf.CreatedOn DESC;
END
GO
