-- usp_InsertSupportTicket
-- Called by the Relay webhook endpoint.
-- Resolves UserId, ClientId, and AssignedTo from the sender email, then
-- inserts the full enriched ticket row.
--
-- Assignment logic (only applies when the sender is a known client user):
--   SenderEmail → UserList → ClientUser → TransClientUser (TOP 1 active IRF)
--                          → IRF.RisebirdHr → UserList.EmailId = AssignedTo
CREATE OR ALTER PROCEDURE [dbo].[usp_InsertSupportTicket]
    @SenderEmail                VARCHAR(150),
    @Subject                    NVARCHAR(500),
    @MailContent                NVARCHAR(MAX),
    @ReceivedAt                 DATETIME,
    @Intent                     NVARCHAR(500),
    @PrimarySpecialization      NVARCHAR(500)  = NULL,
    @MailBodyIntent             NVARCHAR(500)  = NULL,
    @AttachmentJobDescription   BIT            = 0,
    @AttachmentPdf              BIT            = 0,
    @AttachmentResumes          BIT            = 0,
    @DriveFolderLink            NVARCHAR(1000) = NULL,
    @SummaryMail                NVARCHAR(MAX)  = NULL,
    @SummaryCandidates          NVARCHAR(MAX)  = NULL,
    @SummaryInterviewScores     NVARCHAR(MAX)  = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserId         INT          = NULL;
    DECLARE @ClientId       INT          = NULL;
    --DECLARE @ClientUserId   INT          = NULL;
    DECLARE @IRFId          INT          = NULL;
    DECLARE @RisebirdHrId   INT          = NULL;
    DECLARE @AssignedTo     VARCHAR(150) = NULL;

    -- Step 1: Resolve UserId from sender email
    SELECT TOP 1 @UserId = id
    FROM   dbo.UserList
    WHERE  LOWER(EmailId) = LOWER(@SenderEmail)
      AND  IsActive = 1;

    IF @UserId IS NOT NULL
    BEGIN
        -- Step 2: Resolve ClientId and the ClientUser row id
        SELECT TOP 1
            @ClientId     = ClientId--,
            --@ClientUserId = id
        FROM  dbo.ClientUser
        WHERE UserId = @UserId;

        IF @ClientId IS NOT NULL
        BEGIN
            -- Step 3: Find the first active IRF this client user belongs to.
            -- Both the TransClientUser membership AND the IRF (skill channel)
            -- itself must be active (not paused) to be a valid candidate.
            SELECT TOP 1 @IRFId = tcu.IRFId
            FROM   dbo.TransClientUser tcu
            INNER JOIN dbo.IRF         irf ON irf.id = tcu.IRFId
            WHERE  tcu.ClientUserId = @UserId
              AND  tcu.IsActive     = 1
              AND  irf.IsPaused     = 0
            ORDER  BY irf.CreatedOn desc;

            IF @IRFId IS NOT NULL
            BEGIN
                -- Step 4: Get the RisebirdHr (internal HR) for that IRF.
                -- No extra filter needed here — the IRFId was already
                -- validated as non-paused in Step 3.
                SELECT TOP 1 @RisebirdHrId = RisebirdHr
                FROM   dbo.IRF
                WHERE  id = @IRFId;

                IF @RisebirdHrId IS NOT NULL
                BEGIN
                    -- Step 5: Resolve the HR's email address
                    SELECT TOP 1 @AssignedTo = EmailId
                    FROM   dbo.UserList
                    WHERE  id = @RisebirdHrId
                      AND  IsActive = 1;
                END
            END
        END
    END

    INSERT INTO dbo.SupportTickets (
        SenderEmail, Subject, MailContent, ReceivedAt,
        Intent, PrimarySpecialization, MailBodyIntent,
        AttachmentJobDescription, AttachmentPdf, AttachmentResumes,
        DriveFolderLink, SummaryMail, SummaryCandidates, SummaryInterviewScores,
        UserId, ClientId, AssignedTo
    ) VALUES (
        @SenderEmail, @Subject, @MailContent, @ReceivedAt,
        @Intent, @PrimarySpecialization, @MailBodyIntent,
        @AttachmentJobDescription, @AttachmentPdf, @AttachmentResumes,
        @DriveFolderLink, @SummaryMail, @SummaryCandidates, @SummaryInterviewScores,
        @UserId, @ClientId, @AssignedTo
    );

    SELECT SCOPE_IDENTITY() AS NewId;
END
GO
