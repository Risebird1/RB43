IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_UpdateNaukriManualOverride')
    DROP PROCEDURE usp_UpdateNaukriManualOverride
GO

CREATE PROCEDURE [dbo].[usp_UpdateNaukriManualOverride]
    @Id             INT,
    @FieldName      VARCHAR(50),
    @OldValue       NVARCHAR(500)   = NULL,
    @NewValue       NVARCHAR(500),
    @ChangedBy      VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    -- Append to the JSON audit log
    DECLARE @LogEntry NVARCHAR(MAX) = '{"field":"' + @FieldName + '","from":' +
        CASE WHEN @OldValue IS NULL THEN 'null' ELSE '"' + REPLACE(@OldValue, '"', '\"') + '"' END +
        ',"to":"' + REPLACE(@NewValue, '"', '\"') +
        '","by":"' + REPLACE(@ChangedBy, '"', '\"') +
        '","at":"' + CONVERT(VARCHAR(30), GETUTCDATE(), 127) + '"}'

    DECLARE @CurrentLog NVARCHAR(MAX)
    SELECT @CurrentLog = ModificationLog FROM NaukriProspectiveInterviewer WHERE Id = @Id

    DECLARE @NewLog NVARCHAR(MAX)
    IF @CurrentLog IS NULL OR LEN(LTRIM(RTRIM(@CurrentLog))) < 3
        SET @NewLog = '[' + @LogEntry + ']'
    ELSE
        SET @NewLog = LEFT(@CurrentLog, LEN(@CurrentLog) - 1) + ',' + @LogEntry + ']'

    -- Update the actual field
    IF @FieldName = 'MagName'
    BEGIN
        UPDATE NaukriProspectiveInterviewer
        SET MagName = @NewValue, ModificationLog = @NewLog, UpdatedAt = GETUTCDATE()
        WHERE Id = @Id
    END
    ELSE IF @FieldName = 'ParsedEmail'
    BEGIN
        -- Re-check if the new email exists in the system
        DECLARE @ExUserId INT = NULL, @ExProfileId INT = NULL, @IsEx BIT = 0
        SELECT TOP 1 @ExUserId = ul.id, @ExProfileId = ip.Id
        FROM UserList ul
        LEFT JOIN InterviewerProfile ip ON ip.UserId = ul.id
        WHERE LOWER(ul.EmailId) = LOWER(@NewValue) AND ul.IsActive = 1

        IF @ExUserId IS NOT NULL SET @IsEx = 1

        UPDATE NaukriProspectiveInterviewer
        SET ParsedEmail = @NewValue,
            IsExistingInterviewer = @IsEx,
            ExistingUserId = @ExUserId,
            ExistingProfileId = @ExProfileId,
            ModificationLog = @NewLog,
            UpdatedAt = GETUTCDATE()
        WHERE Id = @Id

        SELECT @IsEx AS IsExisting, @ExUserId AS ExistingUserId, @ExProfileId AS ExistingProfileId
        RETURN
    END
    ELSE IF @FieldName = 'ParsedPhone'
    BEGIN
        UPDATE NaukriProspectiveInterviewer
        SET ParsedPhone = @NewValue, ModificationLog = @NewLog, UpdatedAt = GETUTCDATE()
        WHERE Id = @Id
    END

    SELECT 1 AS Success
END
GO
