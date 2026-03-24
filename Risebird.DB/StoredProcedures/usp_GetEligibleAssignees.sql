-- usp_GetEligibleAssignees
-- Given a TicketId, resolves the sender's client user and returns the distinct
-- EmailIds of all active RisebirdHR users assigned to any active, non-paused IRF
-- that the sender belongs to (via TransClientUser).
-- Returns an empty result set when the sender cannot be resolved to a client user.
CREATE OR ALTER PROCEDURE [dbo].[usp_GetEligibleAssignees]
    @TicketId INT
AS
BEGIN  
    SET NOCOUNT ON;  
  
    DECLARE @SenderEmail  VARCHAR(150);  
    DECLARE @UserId       INT;  
    --DECLARE @ClientUserId INT;  
  
    -- Step 1: Get the sender email from the ticket  
    SELECT TOP 1 @SenderEmail = SenderEmail  
    FROM   dbo.SupportTickets  
    WHERE  id = @TicketId;  
  
    IF @SenderEmail IS NOT NULL  
    BEGIN  
        -- Step 2: Resolve the UserList row for the sender  
        SELECT TOP 1 @UserId = id  
        FROM   dbo.UserList  
        WHERE  LOWER(EmailId) = LOWER(@SenderEmail)  
          AND  IsActive = 1;  
  
        IF @UserId IS NOT NULL  
        BEGIN  
            SELECT DISTINCT ul.EmailId  
                FROM   dbo.TransClientUser tcu  
                INNER JOIN dbo.IRF         irf ON irf.id = tcu.IRFId  
                INNER JOIN dbo.UserList    ul  ON ul.id  = irf.RisebirdHr  
                WHERE  tcu.ClientUserId = @UserId  
                  AND  tcu.IsActive     = 1  
                  AND  irf.IsPaused     = 0  
                  AND  ul.IsActive      = 1  
                ORDER  BY ul.EmailId;  
  
                RETURN;  
        END  
    END  
  
    -- Sender not resolvable to a client user — return empty set  
    SELECT TOP 0 CAST(NULL AS VARCHAR(150)) AS EmailId;  
END
GO
