-- usp_FindBestIRFForClient
-- Finds the best matching IRF for a sender's client based on job title/specialization.
-- Unlike usp_AutoMapUserToIRF, this works even if user already has existing mappings.
-- Use this when existing mappings don't match well (score = 0) and we need to find a better fit.
--
-- Parameters:
--   @SenderEmail      - Email of the sender (to resolve client)
--   @JobTitle         - Job title to match against
--   @Specialization   - Optional specialization to add to search
--   @CreateMapping    - If 1 and a match is found, creates a new TransClientUser entry
--
-- Returns:
--   Best matching IRF details with WasAutoMapped = 1 if new mapping was created
--
-- NOTE: TransClientUser.ClientUserId references UserList.id (not ClientUser.id)
CREATE OR ALTER PROCEDURE [dbo].[usp_FindBestIRFForClient]
    @SenderEmail    VARCHAR(150),
    @JobTitle       NVARCHAR(500),
    @Specialization NVARCHAR(500) = NULL,
    @CreateMapping  BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserId       INT = NULL;
    DECLARE @ClientId     INT = NULL;
    DECLARE @ClientRoleId INT = NULL;
    DECLARE @MatchedIRFId INT = NULL;
    DECLARE @AlreadyMapped BIT = 0;

    -- Step 1: Resolve UserList row for the sender
    SELECT TOP 1 @UserId = id
    FROM   dbo.UserList
    WHERE  LOWER(EmailId) = LOWER(@SenderEmail)
      AND  IsActive = 1;

    IF @UserId IS NULL
    BEGIN
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

    -- Step 3: Find the best matching IRF for this client
    DECLARE @SearchText NVARCHAR(1000) = LOWER(LTRIM(RTRIM(ISNULL(@JobTitle, '') + ' ' + ISNULL(@Specialization, ''))));

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
            -- Score: higher = better match
            CASE 
                WHEN @SearchText LIKE '%' + LOWER(LTRIM(RTRIM(ISNULL(irf.JDTiltle, '')))) + '%' THEN 100
                WHEN LOWER(ISNULL(irf.JDTiltle, '')) LIKE '%' + @SearchText + '%' THEN 100
                ELSE 0
            END +
            CASE 
                WHEN @SearchText LIKE '%' + LOWER(LTRIM(RTRIM(ISNULL(irf.SkillCluster, '')))) + '%' THEN 50
                WHEN LOWER(ISNULL(irf.SkillCluster, '')) LIKE '%' + @SearchText + '%' THEN 50
                ELSE 0
            END AS MatchScore
        FROM   dbo.IRF irf
        INNER JOIN dbo.UserList ul ON ul.id = irf.RisebirdHr
        WHERE  irf.ClientId  = @ClientId
          AND  irf.IsPaused  = 0
          AND  irf.IsDeleted = 0
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
        -- No matching IRF found for this client
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

    -- Step 4: Check if user is already mapped to this specific IRF
    IF EXISTS (
        SELECT 1 
        FROM   dbo.TransClientUser 
        WHERE  ClientUserId = @UserId
          AND  IRFId = @MatchedIRFId
          AND  IsActive = 1
    )
    BEGIN
        SET @AlreadyMapped = 1;
    END

    -- Step 5: Create mapping if requested and not already mapped
    -- Mark as auto-mapped by the resume ingestion flow
    IF @CreateMapping = 1 AND @AlreadyMapped = 0
    BEGIN
        INSERT INTO dbo.TransClientUser (IRFId, ClientUserId, ClientRoleId, IsActive, IsAutoMapped, AutoMappedOn)
        VALUES (@MatchedIRFId, @UserId, ISNULL(@ClientRoleId, 1), 1, 1, GETUTCDATE());
        SET @AlreadyMapped = 0; -- Indicates we created a new mapping
    END
    ELSE
    BEGIN
        SET @AlreadyMapped = 1; -- Either already mapped or we didn't create
    END

    -- Step 6: Return the matched IRF
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
        CASE WHEN @AlreadyMapped = 0 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS WasAutoMapped
    FROM   dbo.IRF irf
    INNER JOIN dbo.UserList ul ON ul.id = irf.RisebirdHr
    WHERE  irf.id = @MatchedIRFId;
END
GO
