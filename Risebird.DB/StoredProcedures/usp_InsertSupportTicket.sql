-- usp_InsertSupportTicket
-- Called by the Relay webhook endpoint. Resolves UserId + ClientId from
-- the sender email, then inserts the full enriched ticket row.
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

    DECLARE @UserId   INT = NULL;
    DECLARE @ClientId INT = NULL;

    SELECT TOP 1 @UserId = id
    FROM   dbo.UserList
    WHERE  LOWER(EmailId) = LOWER(@SenderEmail)
      AND  IsActive = 1;

    IF @UserId IS NOT NULL
    BEGIN
        SELECT TOP 1 @ClientId = ClientId
        FROM   dbo.ClientUser
        WHERE  UserId = @UserId;
    END

    INSERT INTO dbo.SupportTickets (
        SenderEmail, Subject, MailContent, ReceivedAt,
        Intent, PrimarySpecialization, MailBodyIntent,
        AttachmentJobDescription, AttachmentPdf, AttachmentResumes,
        DriveFolderLink, SummaryMail, SummaryCandidates, SummaryInterviewScores,
        UserId, ClientId
    ) VALUES (
        @SenderEmail, @Subject, @MailContent, @ReceivedAt,
        @Intent, @PrimarySpecialization, @MailBodyIntent,
        @AttachmentJobDescription, @AttachmentPdf, @AttachmentResumes,
        @DriveFolderLink, @SummaryMail, @SummaryCandidates, @SummaryInterviewScores,
        @UserId, @ClientId
    );

    SELECT SCOPE_IDENTITY() AS NewId;
END
GO
