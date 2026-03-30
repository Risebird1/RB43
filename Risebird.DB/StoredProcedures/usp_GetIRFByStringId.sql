-- usp_GetIRFByStringId
-- Confirms a Skill Channel exists in IRF by the string IRFId column (e.g. QUADRJAVA5).
-- Used when the sender supplies skill_channel_id in the webhook — no TransClientUser
-- filter; only validates the row exists and is usable (active HR, not paused, in date range).
CREATE OR ALTER PROCEDURE [dbo].[usp_GetIRFByStringId]
    @IRFStringId VARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    IF @IRFStringId IS NULL OR LEN(LTRIM(RTRIM(@IRFStringId))) = 0
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
            CAST(NULL AS DATETIME)     AS CreatedOn;
        RETURN;
    END

    DECLARE @Norm VARCHAR(200) = LTRIM(RTRIM(@IRFStringId));

    SELECT TOP 1
        irf.id                           AS Id,
        irf.IRFId                        AS IRFStringId,
        ISNULL(irf.JDTiltle,     '')     AS JDTiltle,
        ISNULL(irf.SkillCluster, '')     AS SkillCluster,
        irf.RisebirdHr                   AS RisebirdHrId,
        ISNULL(ul.EmailId,       '')     AS RisebirdHrEmail,
        irf.StartDate                    AS StartDate,
        irf.EndDate                      AS EndDate,
        irf.CreatedOn                    AS CreatedOn
    FROM   dbo.IRF      irf
    INNER JOIN dbo.UserList ul ON ul.id = irf.RisebirdHr
    WHERE  LOWER(irf.IRFId) = LOWER(@Norm)
      AND  irf.IsPaused     = 0
      AND  ul.IsActive      = 1
      AND  CAST(GETUTCDATE() AS DATE) >= CAST(irf.StartDate AS DATE)
      AND  CAST(GETUTCDATE() AS DATE) <= CAST(irf.EndDate   AS DATE);
END
GO
