-- Returns the RisebirdHR email for a sender based on their client's existing IRF mappings.
-- Used when we need to assign a ticket to an HR but don't need to match a specific IRF.
-- NOTE: TransClientUser.ClientUserId references UserList.id (not ClientUser.id)
CREATE OR ALTER PROCEDURE [dbo].[usp_GetHRForSender]
    @SenderEmail VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserId    INT = NULL;
    DECLARE @ClientId  INT = NULL;

    -- Step 1: Resolve UserId from UserList
    SELECT TOP 1 @UserId = ul.id
    FROM   dbo.UserList ul
    WHERE  ul.EmailId = @SenderEmail
      AND  ul.IsActive = 1;

    IF @UserId IS NULL
    BEGIN
        SELECT NULL AS RisebirdHrEmail;
        RETURN;
    END

    -- Step 2: Get ClientId from ClientUser
    SELECT TOP 1 @ClientId = cu.ClientId
    FROM   dbo.ClientUser cu
    WHERE  cu.UserId = @UserId;

    IF @ClientId IS NULL
    BEGIN
        SELECT NULL AS RisebirdHrEmail;
        RETURN;
    END

    -- Step 3: Find the HR from the most recent active IRF for this client
    -- Prioritize by EndDate (most recent first), then StartDate, then CreatedOn
    SELECT TOP 1
        ul_hr.EmailId AS RisebirdHrEmail
    FROM   dbo.IRF irf
    INNER JOIN dbo.UserList ul_hr ON ul_hr.id = irf.RisebirdHr AND ul_hr.IsActive = 1
    WHERE  irf.ClientId  = @ClientId
      AND  irf.IsPaused  = 0
      AND  irf.IsDeleted = 0
      AND  GETDATE() BETWEEN irf.StartDate AND irf.EndDate
    ORDER BY irf.EndDate DESC, irf.StartDate DESC, irf.CreatedOn DESC;
END
GO
