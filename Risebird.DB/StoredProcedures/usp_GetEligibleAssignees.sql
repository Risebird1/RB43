-- usp_GetEligibleAssignees
-- Given a TicketId, resolves the sender's client user and returns the distinct
-- EmailIds of all active RisebirdHR users assigned to any active, non-paused IRF
-- that the sender belongs to (via TransClientUser).
-- Returns an empty result set when the sender cannot be resolved to a client user.
--
-- SPECIAL CASE: If the sender is a Client Admin (RoleId = 9), returns all HRs
-- from ALL active IRFs for their client (not just mapped IRFs).
CREATE OR ALTER PROCEDURE [dbo].[usp_GetEligibleAssignees]
    @TicketId INT
AS
BEGIN  
    SET NOCOUNT ON;  
  
    DECLARE @SenderEmail  VARCHAR(150);  
    DECLARE @UserId       INT;
    DECLARE @ClientId     INT;
    DECLARE @RoleId       INT;
  
    -- Step 1: Get the sender email from the ticket  
    SELECT TOP 1 @SenderEmail = SenderEmail  
    FROM   dbo.SupportTickets  
    WHERE  id = @TicketId;  
  
    IF @SenderEmail IS NULL
    BEGIN
        SELECT TOP 0 CAST(NULL AS VARCHAR(150)) AS EmailId;
        RETURN;
    END

    -- Step 2: Resolve the UserList row for the sender  
    SELECT TOP 1 @UserId = id  
    FROM   dbo.UserList  
    WHERE  LOWER(EmailId) = LOWER(@SenderEmail)  
      AND  IsActive = 1;  
  
    IF @UserId IS NULL
    BEGIN
        SELECT TOP 0 CAST(NULL AS VARCHAR(150)) AS EmailId;
        RETURN;
    END

    -- Step 3: Check if user is a Client Admin (RoleId = 9)
    SELECT TOP 1 @ClientId = cu.ClientId, @RoleId = cu.RoleId
    FROM   dbo.ClientUser cu
    WHERE  cu.UserId = @UserId;

    -- If Client Admin (RoleId = 9), return HRs from ALL client IRFs
    IF @RoleId = 9 AND @ClientId IS NOT NULL
    BEGIN
        SELECT DISTINCT ul.EmailId  
        FROM   dbo.IRF irf
        INNER JOIN dbo.UserList ul ON ul.id = irf.RisebirdHr  
        WHERE  irf.ClientId = @ClientId
          AND  irf.IsPaused = 0
          AND  ISNULL(irf.IsDeleted, 0) = 0
          AND  ul.IsActive  = 1
        ORDER  BY ul.EmailId;
        RETURN;
    END

    -- Step 4: Regular users - get HRs from mapped IRFs only
    SELECT DISTINCT ul.EmailId  
    FROM   dbo.TransClientUser tcu  
    INNER JOIN dbo.IRF         irf ON irf.id = tcu.IRFId  
    INNER JOIN dbo.UserList    ul  ON ul.id  = irf.RisebirdHr  
    WHERE  tcu.ClientUserId = @UserId  
      AND  tcu.IsActive     = 1  
      AND  irf.IsPaused     = 0  
      AND  ul.IsActive      = 1  
    ORDER  BY ul.EmailId;
END
GO
