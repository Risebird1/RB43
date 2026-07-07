IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_InsertNaukriProspectiveInterviewer')
    DROP PROCEDURE usp_InsertNaukriProspectiveInterviewer
GO

CREATE PROCEDURE [dbo].[usp_InsertNaukriProspectiveInterviewer]
    @SenderEmail            VARCHAR(150),
    @Subject                NVARCHAR(500)   = NULL,
    @MailContent            NVARCHAR(MAX)   = NULL,
    @ReceivedAt             DATETIME        = NULL,
    @CandidateName          NVARCHAR(200)   = NULL,
    @Location               NVARCHAR(200)   = NULL,
    @PastExperience         NVARCHAR(500)   = NULL,
    @NoticePeriod           NVARCHAR(100)   = NULL,
    @Education              NVARCHAR(500)   = NULL,
    @KeySkills              NVARCHAR(MAX)   = NULL,
    @CurrentRole            NVARCHAR(200)   = NULL,
    @ExperienceYears        NVARCHAR(50)    = NULL,
    @Salary                 NVARCHAR(50)    = NULL,
    @MagName                NVARCHAR(200)   = NULL,
    @ParsedEmail            VARCHAR(200)    = NULL,
    @ParsedPhone            VARCHAR(50)     = NULL,
    @ResumeParseCompleted   BIT             = NULL,
    @ResumeLink             NVARCHAR(2000)  = NULL,
    @HasAttachment          BIT             = 0,
    @Source                 VARCHAR(50)     = 'Naukri',
    @NewId                  INT             OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1 FROM NaukriProspectiveInterviewer
        WHERE LOWER(LTRIM(RTRIM(ISNULL(CandidateName,'')))) = LOWER(LTRIM(RTRIM(ISNULL(@CandidateName,''))))
          AND LOWER(LTRIM(RTRIM(ISNULL(Subject,'')))) = LOWER(LTRIM(RTRIM(ISNULL(@Subject,''))))
    )
    BEGIN
        SELECT @NewId = Id FROM NaukriProspectiveInterviewer
        WHERE LOWER(LTRIM(RTRIM(ISNULL(CandidateName,'')))) = LOWER(LTRIM(RTRIM(ISNULL(@CandidateName,''))))
          AND LOWER(LTRIM(RTRIM(ISNULL(Subject,'')))) = LOWER(LTRIM(RTRIM(ISNULL(@Subject,''))))
        ORDER BY Id DESC

        IF @ResumeLink IS NOT NULL AND LEN(LTRIM(RTRIM(@ResumeLink))) > 0
           AND @ResumeLink <> 'None'
        BEGIN
            UPDATE NaukriProspectiveInterviewer
            SET ResumeLink = @ResumeLink, HasAttachment = @HasAttachment, UpdatedAt = GETUTCDATE()
            WHERE Id = @NewId
              AND (ResumeLink IS NULL OR ResumeLink = 'None' OR LEN(LTRIM(RTRIM(ResumeLink))) = 0)
        END

        -- On duplicate, still update email/phone if Relay now provides them and they were missing
        IF @ParsedEmail IS NOT NULL AND LEN(LTRIM(RTRIM(@ParsedEmail))) > 0
        BEGIN
            UPDATE NaukriProspectiveInterviewer
            SET ParsedEmail = @ParsedEmail, UpdatedAt = GETUTCDATE()
            WHERE Id = @NewId AND (ParsedEmail IS NULL OR LEN(LTRIM(RTRIM(ParsedEmail))) = 0)
        END

        IF @ParsedPhone IS NOT NULL AND LEN(LTRIM(RTRIM(@ParsedPhone))) > 0
        BEGIN
            UPDATE NaukriProspectiveInterviewer
            SET ParsedPhone = @ParsedPhone, UpdatedAt = GETUTCDATE()
            WHERE Id = @NewId AND (ParsedPhone IS NULL OR LEN(LTRIM(RTRIM(ParsedPhone))) = 0)
        END

        IF @ResumeParseCompleted = 1
        BEGIN
            UPDATE NaukriProspectiveInterviewer
            SET ResumeParseCompleted = 1, UpdatedAt = GETUTCDATE()
            WHERE Id = @NewId AND ISNULL(ResumeParseCompleted, 0) = 0
        END

        RETURN
    END

    INSERT INTO NaukriProspectiveInterviewer
        (SenderEmail, Subject, MailContent, ReceivedAt,
         CandidateName, Location, PastExperience, NoticePeriod, Education,
         KeySkills, CurrentRole, ExperienceYears, Salary, MagName,
         ParsedEmail, ParsedPhone, ResumeParseCompleted,
         ResumeLink, HasAttachment, [Source], Status, CreatedAt)
    VALUES
        (@SenderEmail, @Subject, @MailContent, ISNULL(@ReceivedAt, GETUTCDATE()),
         @CandidateName, @Location, @PastExperience, @NoticePeriod, @Education,
         @KeySkills, @CurrentRole, @ExperienceYears, @Salary, @MagName,
         @ParsedEmail, @ParsedPhone, @ResumeParseCompleted,
         @ResumeLink, @HasAttachment, ISNULL(@Source, 'Naukri'), 'Open', GETUTCDATE())

    SET @NewId = SCOPE_IDENTITY()

    -- Ticket-style ownership: assign the new prospect to a random eligible operator so someone is
    -- accountable for its closure from the moment it enters the pipeline.
    EXEC dbo.usp_AssignNaukriProspectRandomly @Id = @NewId;
END
GO
