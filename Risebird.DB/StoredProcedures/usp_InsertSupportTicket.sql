-- usp_InsertSupportTicket
-- Called by the Relay webhook endpoint.
-- Looks up UserId from UserList by SenderEmail, then looks up ClientId
-- from ClientUser by UserId. Inserts the ticket and returns the new Id.
CREATE OR ALTER PROCEDURE [dbo].[usp_InsertSupportTicket]
    @SenderEmail  VARCHAR(150),
    @Subject      NVARCHAR(500),
    @MailContent  NVARCHAR(MAX),
    @ReceivedAt   DATETIME,
    @Intent       NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserId   INT = NULL;
    DECLARE @ClientId INT = NULL;

    -- Resolve user by email (case-insensitive)
    SELECT TOP 1 @UserId = id
    FROM   dbo.UserList
    WHERE  LOWER(EmailId) = LOWER(@SenderEmail)
      AND  IsActive = 1;

    -- Resolve client via ClientUser mapping
    IF @UserId IS NOT NULL
    BEGIN
        SELECT TOP 1 @ClientId = ClientId
        FROM   dbo.ClientUser
        WHERE  UserId = @UserId;
    END

    INSERT INTO dbo.SupportTickets
        (SenderEmail, Subject, MailContent, ReceivedAt, Intent, UserId, ClientId)
    VALUES
        (@SenderEmail, @Subject, @MailContent, @ReceivedAt, @Intent, @UserId, @ClientId);

    SELECT SCOPE_IDENTITY() AS NewId;
END
GO
