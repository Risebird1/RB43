-- usp_InsertSupportTicket
-- Called by the Relay webhook endpoint.
-- Resolves UserId, ClientId from the sender email, then inserts the ticket.
--
-- Assignment logic:
--   - For resume ingestion intents ("New candidate submission", "New interview request"):
--     AssignedTo is left NULL. The controller determines assignment after ingestion.
--   - For all other intents: AssignedTo is resolved to the RisebirdHr email.
--
-- NOTE: TransClientUser.ClientUserId references UserList.id, NOT ClientUser.id.
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
    @SummaryInterviewScores     NVARCHAR(MAX)  = NULL,
    @JobTitle                   NVARCHAR(200)  = NULL,
    @HasAutoScheduleCandidates  BIT            = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserId         INT          = NULL;
    DECLARE @ClientId       INT          = NULL;
    DECLARE @IRFId          INT          = NULL;
    DECLARE @RisebirdHrId   INT          = NULL;
    DECLARE @AssignedTo     VARCHAR(150) = NULL;

    -- Check if this is a resume ingestion intent (assignment deferred to controller)
    DECLARE @IsResumeIngestionIntent BIT = 0;
    IF LOWER(@Intent) IN ('new candidate submission', 'new interview request')
        SET @IsResumeIngestionIntent = 1;

    -- Step 1: Resolve UserId from sender email
    SELECT TOP 1 @UserId = id
    FROM   dbo.UserList
    WHERE  LOWER(EmailId) = LOWER(@SenderEmail)
      AND  IsActive = 1;

    IF @UserId IS NOT NULL
    BEGIN
        -- Step 2: Resolve ClientId
        SELECT TOP 1 @ClientId = ClientId
        FROM  dbo.ClientUser
        WHERE UserId = @UserId;

        -- Only resolve HR assignment for non-resume-ingestion intents
        IF @ClientId IS NOT NULL AND @IsResumeIngestionIntent = 0
        BEGIN
            -- Step 3: Find the first active IRF this client user belongs to.
            SELECT TOP 1 @IRFId = tcu.IRFId
            FROM   dbo.TransClientUser tcu
            INNER JOIN dbo.IRF         irf ON irf.id = tcu.IRFId
            WHERE  tcu.ClientUserId = @UserId
              AND  tcu.IsActive     = 1
              AND  irf.IsPaused     = 0
            ORDER  BY irf.CreatedOn DESC;

            IF @IRFId IS NOT NULL
            BEGIN
                -- Step 4: Get the RisebirdHr for that IRF.
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
        UserId, ClientId, AssignedTo, JobTitle, HasAutoScheduleCandidates
    ) VALUES (
        @SenderEmail, @Subject, @MailContent, @ReceivedAt,
        @Intent, @PrimarySpecialization, @MailBodyIntent,
        @AttachmentJobDescription, @AttachmentPdf, @AttachmentResumes,
        @DriveFolderLink, @SummaryMail, @SummaryCandidates, @SummaryInterviewScores,
        @UserId, @ClientId, @AssignedTo, @JobTitle, @HasAutoScheduleCandidates
    );

    SELECT SCOPE_IDENTITY() AS NewId;
END
GO
