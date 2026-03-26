-- usp_GetIRFsForSender
-- Returns all active, non-paused IRFs for a given sender email,
-- including SkillCluster (used for job-title matching) and RisebirdHr
-- (used as AddedBy when inserting CandidateResume rows).
CREATE OR ALTER PROCEDURE [dbo].[usp_GetIRFsForSender]
    @SenderEmail VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserId       INT;
    DECLARE @ClientUserId INT;

    SELECT TOP 1 @UserId = id
    FROM   dbo.UserList
    WHERE  LOWER(EmailId) = LOWER(@SenderEmail)
      AND  IsActive = 1;

    IF @UserId IS NOT NULL
    BEGIN
        SELECT TOP 1 @ClientUserId = id
        FROM   dbo.ClientUser
        WHERE  UserId = @UserId;

        IF @ClientUserId IS NOT NULL
        BEGIN
            SELECT DISTINCT
                irf.id                                 AS Id,
                irf.IRFId                              AS IRFStringId,
                ISNULL(irf.JDTiltle,     '')           AS JDTiltle,
                --ISNULL(irf.SkillCluster, '')           AS SkillCluster,
                irf.RisebirdHr                         AS RisebirdHrId,
                ISNULL(ul.EmailId,       '')           AS RisebirdHrEmail
            FROM   dbo.TransClientUser tcu
            INNER JOIN dbo.IRF         irf ON irf.id  = tcu.IRFId
            INNER JOIN dbo.UserList    ul  ON ul.id   = irf.RisebirdHr
            WHERE  tcu.ClientUserId = @ClientUserId
              AND  tcu.IsActive     = 1
              AND  irf.IsPaused     = 0
              AND  ul.IsActive      = 1
            ORDER  BY irf.id ASC;

            RETURN;
        END
    END

    -- Sender not resolvable to a client user — return empty set
    SELECT TOP 0
        CAST(NULL AS INT)          AS Id,
        CAST(NULL AS VARCHAR(200)) AS IRFStringId,
        CAST(NULL AS VARCHAR(150)) AS JDTiltle,
        CAST(NULL AS VARCHAR(200)) AS SkillCluster,
        CAST(NULL AS INT)          AS RisebirdHrId,
        CAST(NULL AS VARCHAR(150)) AS RisebirdHrEmail;
END
GO
