-- usp_GetEligibleAssignees
-- Given a TicketId, resolves the sender's client user and returns the distinct
-- EmailIds of all active RisebirdHR users assigned to any active, non-paused IRF
-- that the sender belongs to (via TransClientUser).
--
-- SPECIAL CASE: If the sender is a Client Admin (RoleId = 9), returns all HRs
-- from ALL active IRFs for their client (not just mapped IRFs).
--
-- FALLBACK: If no eligible assignees found through normal logic, returns ALL
-- active Risebird HRs from any active IRF. This ensures tickets can always
-- be manually assigned to someone.
CREATE OR ALTER PROCEDURE [dbo].[usp_GetEligibleAssignees]
    @TicketId INT
AS
BEGIN  
    SET NOCOUNT ON;  
  
    DECLARE @SenderEmail  VARCHAR(150);  
    DECLARE @UserId       INT;
    DECLARE @ClientId     INT;
    DECLARE @RoleId       INT;
    
    -- Temp table to store results
    DECLARE @EligibleHRs TABLE (EmailId VARCHAR(150));
  
    -- Step 1: Get the sender email from the ticket  
    SELECT TOP 1 @SenderEmail = SenderEmail  
    FROM   dbo.SupportTickets  
    WHERE  id = @TicketId;  
  
    -- Step 2: Try to resolve the UserList row for the sender  
    IF @SenderEmail IS NOT NULL
    BEGIN
        SELECT TOP 1 @UserId = id  
        FROM   dbo.UserList  
        WHERE  LOWER(EmailId) = LOWER(@SenderEmail)  
          AND  IsActive = 1;  
    END

    -- Step 3: Check if user is a Client Admin (RoleId = 9)
    IF @UserId IS NOT NULL
    BEGIN
        SELECT TOP 1 @ClientId = cu.ClientId, @RoleId = cu.RoleId
        FROM   dbo.ClientUser cu
        WHERE  cu.UserId = @UserId;

        -- If Client Admin (RoleId = 9), get HRs from ALL client IRFs
        IF @RoleId = 9 AND @ClientId IS NOT NULL
        BEGIN
            INSERT INTO @EligibleHRs (EmailId)
            SELECT DISTINCT ul.EmailId  
            FROM   dbo.IRF irf
            INNER JOIN dbo.UserList ul ON ul.id = irf.RisebirdHr  
            WHERE  irf.ClientId = @ClientId
              AND  irf.IsPaused = 0
              AND  ISNULL(irf.IsDeleted, 0) = 0
              AND  ul.IsActive  = 1;
        END
        ELSE
        BEGIN
            -- Step 4: Regular users - get HRs from mapped IRFs only
            INSERT INTO @EligibleHRs (EmailId)
            SELECT DISTINCT ul.EmailId  
            FROM   dbo.TransClientUser tcu  
            INNER JOIN dbo.IRF         irf ON irf.id = tcu.IRFId  
            INNER JOIN dbo.UserList    ul  ON ul.id  = irf.RisebirdHr  
            WHERE  tcu.ClientUserId = @UserId  
              AND  tcu.IsActive     = 1  
              AND  irf.IsPaused     = 0  
              AND  ul.IsActive      = 1;
        END
    END

    -- Step 5: FALLBACK - If no eligible HRs found, return ALL active Risebird HRs
    -- This ensures tickets can always be assigned to someone
    IF NOT EXISTS (SELECT 1 FROM @EligibleHRs)
    BEGIN
        INSERT INTO @EligibleHRs (EmailId)
        SELECT DISTINCT ul.EmailId
        FROM   dbo.IRF irf
        INNER JOIN dbo.UserList ul ON ul.id = irf.RisebirdHr
        WHERE  irf.IsPaused = 0
          AND  ISNULL(irf.IsDeleted, 0) = 0
          AND  ul.IsActive = 1;
    END

    -- Return results
    SELECT DISTINCT EmailId
    FROM   @EligibleHRs
    ORDER  BY EmailId;
END
GO
