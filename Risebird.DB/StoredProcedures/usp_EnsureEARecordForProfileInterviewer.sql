CREATE OR ALTER PROCEDURE [dbo].[usp_EnsureEARecordForProfileInterviewer]
    @InterviewerProfileId INT,
    @MagName              NVARCHAR(200),
    @Source               VARCHAR(50)    = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ExistingId INT;

    SELECT TOP 1 @ExistingId = ea.Id
    FROM InterviewerEmailAvailability ea
    WHERE ea.InterviewerProfileId = @InterviewerProfileId
      AND ea.MagName = @MagName
      AND ea.Status IN ('Open', 'InProgress', 'Verified')
    ORDER BY ea.Id DESC;

    IF @ExistingId IS NOT NULL
    BEGIN
        SELECT @ExistingId AS Id;
        RETURN;
    END

    DECLARE @UserId INT, @Email VARCHAR(150), @Phone VARCHAR(50), @Name NVARCHAR(200);

    SELECT @UserId = ip.UserId,
           @Name   = ip.NickName,
           @Phone  = ul.MobileNumber,
           @Email  = ul.EmailId
    FROM InterviewerProfile ip
    INNER JOIN UserList ul ON ul.Id = ip.UserId
    WHERE ip.Id = @InterviewerProfileId;

    INSERT INTO InterviewerEmailAvailability
        (SenderEmail, UserId, InterviewerProfileId, InterviewerName, Phone, MagName, Status, Source, CreatedAt)
    VALUES
        (@Email, @UserId, @InterviewerProfileId, @Name, @Phone, @MagName, 'Open', @Source, GETUTCDATE());

    SELECT SCOPE_IDENTITY() AS Id;
END
GO
