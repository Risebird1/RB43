IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_InsertNaukriProspectiveInterviewer')
    DROP PROCEDURE usp_InsertNaukriProspectiveInterviewer
GO

CREATE PROCEDURE [dbo].[usp_InsertNaukriProspectiveInterviewer]
    @SenderEmail        VARCHAR(150),
    @Subject            NVARCHAR(500)   = NULL,
    @MailContent        NVARCHAR(MAX)   = NULL,
    @ReceivedAt         DATETIME        = NULL,
    @CandidateName      NVARCHAR(200)   = NULL,
    @Location           NVARCHAR(200)   = NULL,
    @PastExperience     NVARCHAR(500)   = NULL,
    @NoticePeriod       NVARCHAR(100)   = NULL,
    @Education          NVARCHAR(500)   = NULL,
    @KeySkills          NVARCHAR(MAX)   = NULL,
    @CurrentRole        NVARCHAR(200)   = NULL,
    @ExperienceYears    NVARCHAR(50)    = NULL,
    @Salary             NVARCHAR(50)    = NULL,
    @MagName            NVARCHAR(200)   = NULL,
    @ResumeLink         NVARCHAR(2000)  = NULL,
    @HasAttachment      BIT             = 0,
    @NewId              INT             OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- Duplicate check: same candidate + subject already exists
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

        -- Update resume link if a newer/better one was provided
        IF @ResumeLink IS NOT NULL AND LEN(LTRIM(RTRIM(@ResumeLink))) > 0
           AND @ResumeLink <> 'None'
        BEGIN
            UPDATE NaukriProspectiveInterviewer
            SET ResumeLink = @ResumeLink, HasAttachment = @HasAttachment, UpdatedAt = GETUTCDATE()
            WHERE Id = @NewId
              AND (ResumeLink IS NULL OR ResumeLink = 'None' OR LEN(LTRIM(RTRIM(ResumeLink))) = 0)
        END

        RETURN
    END

    INSERT INTO NaukriProspectiveInterviewer
        (SenderEmail, Subject, MailContent, ReceivedAt,
         CandidateName, Location, PastExperience, NoticePeriod, Education,
         KeySkills, CurrentRole, ExperienceYears, Salary, MagName,
         ResumeLink, HasAttachment, Status, CreatedAt)
    VALUES
        (@SenderEmail, @Subject, @MailContent, ISNULL(@ReceivedAt, GETUTCDATE()),
         @CandidateName, @Location, @PastExperience, @NoticePeriod, @Education,
         @KeySkills, @CurrentRole, @ExperienceYears, @Salary, @MagName,
         @ResumeLink, @HasAttachment, 'Open', GETUTCDATE())

    SET @NewId = SCOPE_IDENTITY()
END
GO
