IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_InsertInterviewerEmailAvailability')
    DROP PROCEDURE usp_InsertInterviewerEmailAvailability
GO

CREATE PROCEDURE [dbo].[usp_InsertInterviewerEmailAvailability]
    @SenderEmail            VARCHAR(150),
    @Subject                NVARCHAR(500)   = NULL,
    @MailContent            NVARCHAR(MAX)   = NULL,
    @ReceivedAt             DATETIME        = NULL,
    @JobTitle               NVARCHAR(200)   = NULL,
    @InterviewerName        NVARCHAR(200)   = NULL,
    @Phone                  VARCHAR(50)     = NULL,
    @MagName                NVARCHAR(200)   = NULL,
    @IRFReference           NVARCHAR(100)   = NULL,
    @HasLoginCredentials    VARCHAR(10)     = NULL,
    @SlotsPerDay            VARCHAR(20)     = NULL,
    @TimeFrame              NVARCHAR(500)   = NULL,
    @AvailabilityIntent     NVARCHAR(MAX)   = NULL,
    @MailBodyIntent         NVARCHAR(500)   = NULL,
    @ConcernsOrNotInterested NVARCHAR(MAX)  = NULL,
    @ResumeFitment          VARCHAR(50)     = NULL,
    @ResumeLink             NVARCHAR(2000)  = NULL,
    @HasAttachment          BIT             = 0,
    @NewId                  INT             OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserId INT = NULL
    DECLARE @InterviewerProfileId INT = NULL

    SELECT TOP 1 @UserId = ul.id, @InterviewerProfileId = ip.Id
    FROM UserList ul
    LEFT JOIN InterviewerProfile ip ON ip.UserId = ul.id
    WHERE ul.EmailId = @SenderEmail AND ul.IsActive = 1

    INSERT INTO InterviewerEmailAvailability
        (SenderEmail, UserId, InterviewerProfileId, Subject, MailContent, ReceivedAt,
         JobTitle, InterviewerName, Phone, MagName, IRFReference, HasLoginCredentials,
         SlotsPerDay, TimeFrame, AvailabilityIntent, MailBodyIntent,
         ConcernsOrNotInterested, ResumeFitment, ResumeLink, HasAttachment, Status, CreatedAt)
    VALUES
        (@SenderEmail, @UserId, @InterviewerProfileId, @Subject, @MailContent,
         ISNULL(@ReceivedAt, GETUTCDATE()),
         @JobTitle, @InterviewerName, @Phone, @MagName, @IRFReference, @HasLoginCredentials,
         @SlotsPerDay, @TimeFrame, @AvailabilityIntent, @MailBodyIntent,
         @ConcernsOrNotInterested, @ResumeFitment, @ResumeLink, @HasAttachment,
         'Open', GETUTCDATE())

    SET @NewId = SCOPE_IDENTITY()
END
GO
