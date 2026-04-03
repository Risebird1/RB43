-- usp_AutoMapUserToIRF
-- Auto-maps a client user to a matching IRF when no existing mapping exists.
-- 
-- Conditions:
--   1. Sender exists in UserList (active)
--   2. Sender exists in ClientUser (has a client association)
--   3. Sender is NOT mapped to any IRF in TransClientUser
--   4. There exists an IRF for the same client that:
--      - Is not paused
--      - Is within date range
--      - Has an active HR
--      - Matches the job title (scored by word overlap)
--
-- If a match is found:
--   - Inserts a new TransClientUser mapping
--   - Returns the matched IRF details
--
-- NOTE: TransClientUser.ClientUserId references UserList.id (not ClientUser.id)
CREATE OR ALTER PROCEDURE [dbo].[usp_AutoMapUserToIRF]
    @SenderEmail    VARCHAR(150),
    @JobTitle       NVARCHAR(500),
    @Specialization NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserId       INT = NULL;
    DECLARE @ClientId     INT = NULL;
    DECLARE @ClientRoleId INT = NULL;
    DECLARE @MatchedIRFId INT = NULL;

    -- Step 1: Resolve UserList row for the sender
    -- NOTE: TransClientUser.ClientUserId stores UserList.id (not ClientUser.id)
    SELECT TOP 1 @UserId = id
    FROM   dbo.UserList
    WHERE  LOWER(EmailId) = LOWER(@SenderEmail)
      AND  IsActive = 1;

    IF @UserId IS NULL
    BEGIN
        -- Sender not in system
        SELECT TOP 0
            CAST(NULL AS INT)          AS Id,
            CAST(NULL AS VARCHAR(200)) AS IRFStringId,
            CAST(NULL AS VARCHAR(150)) AS JDTiltle,
            CAST(NULL AS VARCHAR(200)) AS SkillCluster,
            CAST(NULL AS INT)          AS RisebirdHrId,
            CAST(NULL AS VARCHAR(150)) AS RisebirdHrEmail,
            CAST(NULL AS DATETIME)     AS StartDate,
            CAST(NULL AS DATETIME)     AS EndDate,
            CAST(NULL AS DATETIME)     AS CreatedOn,
            CAST(0 AS BIT)             AS WasAutoMapped;
        RETURN;
    END

    -- Step 2: Get ClientUser info (ClientId, RoleId)
    SELECT TOP 1 
        @ClientId     = cu.ClientId,
        @ClientRoleId = cu.RoleId
    FROM   dbo.ClientUser cu
    WHERE  cu.UserId = @UserId;

    IF @ClientId IS NULL
    BEGIN
        -- User exists but not a client user
        SELECT TOP 0
            CAST(NULL AS INT)          AS Id,
            CAST(NULL AS VARCHAR(200)) AS IRFStringId,
            CAST(NULL AS VARCHAR(150)) AS JDTiltle,
            CAST(NULL AS VARCHAR(200)) AS SkillCluster,
            CAST(NULL AS INT)          AS RisebirdHrId,
            CAST(NULL AS VARCHAR(150)) AS RisebirdHrEmail,
            CAST(NULL AS DATETIME)     AS StartDate,
            CAST(NULL AS DATETIME)     AS EndDate,
            CAST(NULL AS DATETIME)     AS CreatedOn,
            CAST(0 AS BIT)             AS WasAutoMapped;
        RETURN;
    END

    -- Step 3: Check if user already has ANY mapping in TransClientUser
    -- NOTE: ClientUserId in TransClientUser = UserList.id
    IF EXISTS (
        SELECT 1 
        FROM   dbo.TransClientUser 
        WHERE  ClientUserId = @UserId
          AND  IsActive = 1
    )
    BEGIN
        -- User already has mappings - don't auto-map
        SELECT TOP 0
            CAST(NULL AS INT)          AS Id,
            CAST(NULL AS VARCHAR(200)) AS IRFStringId,
            CAST(NULL AS VARCHAR(150)) AS JDTiltle,
            CAST(NULL AS VARCHAR(200)) AS SkillCluster,
            CAST(NULL AS INT)          AS RisebirdHrId,
            CAST(NULL AS VARCHAR(150)) AS RisebirdHrEmail,
            CAST(NULL AS DATETIME)     AS StartDate,
            CAST(NULL AS DATETIME)     AS EndDate,
            CAST(NULL AS DATETIME)     AS CreatedOn,
            CAST(0 AS BIT)             AS WasAutoMapped;
        RETURN;
    END

    -- Step 4: Find a matching IRF for this client
    -- Build a search string from job title and specialization
    DECLARE @SearchText NVARCHAR(1000) = LOWER(ISNULL(@JobTitle, '') + ' ' + ISNULL(@Specialization, ''));

    -- Find the best matching IRF for this client
    ;WITH ScoredIRFs AS (
        SELECT 
            irf.id,
            irf.IRFId                        AS IRFStringId,
            ISNULL(irf.JDTiltle, '')         AS JDTiltle,
            ISNULL(irf.SkillCluster, '')     AS SkillCluster,
            irf.RisebirdHr                   AS RisebirdHrId,
            ISNULL(ul.EmailId, '')           AS RisebirdHrEmail,
            irf.StartDate,
            irf.EndDate,
            irf.CreatedOn,
            -- Simple scoring: check if JDTitle or SkillCluster words appear in search text
            CASE 
                WHEN @SearchText LIKE '%' + LOWER(ISNULL(irf.JDTiltle, '')) + '%' THEN 100
                WHEN LOWER(ISNULL(irf.JDTiltle, '')) LIKE '%' + @SearchText + '%' THEN 100
                ELSE 0
            END +
            CASE 
                WHEN @SearchText LIKE '%' + LOWER(ISNULL(irf.SkillCluster, '')) + '%' THEN 50
                WHEN LOWER(ISNULL(irf.SkillCluster, '')) LIKE '%' + @SearchText + '%' THEN 50
                ELSE 0
            END AS MatchScore
        FROM   dbo.IRF irf
        INNER JOIN dbo.UserList ul ON ul.id = irf.RisebirdHr
        WHERE  irf.ClientId  = @ClientId
          AND  irf.IsPaused  = 0
          AND  ul.IsActive   = 1
          AND  CAST(GETUTCDATE() AS DATE) >= CAST(irf.StartDate AS DATE)
          AND  CAST(GETUTCDATE() AS DATE) <= CAST(irf.EndDate   AS DATE)
    )
    SELECT TOP 1 @MatchedIRFId = id
    FROM   ScoredIRFs
    WHERE  MatchScore > 0
    ORDER  BY MatchScore DESC,
              EndDate DESC,
              StartDate DESC,
              CreatedOn DESC;

    IF @MatchedIRFId IS NULL
    BEGIN
        -- No matching IRF found
        SELECT TOP 0
            CAST(NULL AS INT)          AS Id,
            CAST(NULL AS VARCHAR(200)) AS IRFStringId,
            CAST(NULL AS VARCHAR(150)) AS JDTiltle,
            CAST(NULL AS VARCHAR(200)) AS SkillCluster,
            CAST(NULL AS INT)          AS RisebirdHrId,
            CAST(NULL AS VARCHAR(150)) AS RisebirdHrEmail,
            CAST(NULL AS DATETIME)     AS StartDate,
            CAST(NULL AS DATETIME)     AS EndDate,
            CAST(NULL AS DATETIME)     AS CreatedOn,
            CAST(0 AS BIT)             AS WasAutoMapped;
        RETURN;
    END

    -- Step 5: Auto-mapping DISABLED - do NOT create TransClientUser entry
    -- The SP still finds the best matching IRF but does not insert any mapping.
    -- To re-enable auto-mapping, uncomment the INSERT below.
    /*
    INSERT INTO dbo.TransClientUser (IRFId, ClientUserId, ClientRoleId, IsActive, IsAutoMapped, AutoMappedOn)
    VALUES (@MatchedIRFId, @UserId, ISNULL(@ClientRoleId, 1), 1, 1, GETUTCDATE());
    */

    -- Step 6: Return the matched IRF (WasAutoMapped = 0 since we're not creating mappings)
    SELECT 
        irf.id                           AS Id,
        irf.IRFId                        AS IRFStringId,
        ISNULL(irf.JDTiltle, '')         AS JDTiltle,
        ISNULL(irf.SkillCluster, '')     AS SkillCluster,
        irf.RisebirdHr                   AS RisebirdHrId,
        ISNULL(ul.EmailId, '')           AS RisebirdHrEmail,
        irf.StartDate                    AS StartDate,
        irf.EndDate                      AS EndDate,
        irf.CreatedOn                    AS CreatedOn,
        CAST(0 AS BIT)                   AS WasAutoMapped
    FROM   dbo.IRF irf
    INNER JOIN dbo.UserList ul ON ul.id = irf.RisebirdHr
    WHERE  irf.id = @MatchedIRFId;
END
GO
