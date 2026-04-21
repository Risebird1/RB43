IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_UpdateNaukriResumeParseResult')
    DROP PROCEDURE usp_UpdateNaukriResumeParseResult
GO

CREATE PROCEDURE [dbo].[usp_UpdateNaukriResumeParseResult]
    @Id                     INT,
    @ParsedEmail            VARCHAR(150)    = NULL,
    @ParsedPhone            VARCHAR(50)     = NULL,
    @ResumeParseCompleted   BIT             = 1
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ExistingUserId INT = NULL
    DECLARE @ExistingProfileId INT = NULL
    DECLARE @IsExisting BIT = 0

    IF @ParsedEmail IS NOT NULL AND LEN(LTRIM(RTRIM(@ParsedEmail))) > 0
    BEGIN
        SELECT TOP 1
            @ExistingUserId = ul.id,
            @ExistingProfileId = ip.Id
        FROM UserList ul
        LEFT JOIN InterviewerProfile ip ON ip.UserId = ul.id
        WHERE LOWER(ul.EmailId) = LOWER(@ParsedEmail) AND ul.IsActive = 1

        IF @ExistingUserId IS NOT NULL
            SET @IsExisting = 1
    END

    UPDATE dbo.NaukriProspectiveInterviewer
    SET    ParsedEmail             = @ParsedEmail,
           ParsedPhone             = @ParsedPhone,
           ResumeParseCompleted    = @ResumeParseCompleted,
           IsExistingInterviewer   = @IsExisting,
           ExistingUserId          = @ExistingUserId,
           ExistingProfileId       = @ExistingProfileId,
           Status                  = CASE WHEN Status = 'Open' THEN 'InProgress' ELSE Status END,
           UpdatedAt               = GETUTCDATE()
    WHERE  Id = @Id;

    SELECT @IsExisting AS IsExisting,
           @ExistingUserId AS ExistingUserId,
           @ExistingProfileId AS ExistingProfileId;
END
GO
