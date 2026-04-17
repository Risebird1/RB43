IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_UpdateInterviewerEmailAvailabilityResumeLink')
    DROP PROCEDURE usp_UpdateInterviewerEmailAvailabilityResumeLink
GO

CREATE PROCEDURE [dbo].[usp_UpdateInterviewerEmailAvailabilityResumeLink]
    @Id         INT,
    @ResumeLink NVARCHAR(2000)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @InterviewerProfileId INT = NULL

    SELECT @InterviewerProfileId = InterviewerProfileId
    FROM dbo.InterviewerEmailAvailability
    WHERE Id = @Id

    UPDATE dbo.InterviewerEmailAvailability
    SET    ResumeLink  = @ResumeLink,
           HasAttachment = 1,
           UpdatedAt   = GETUTCDATE()
    WHERE  Id = @Id;

    IF @InterviewerProfileId IS NOT NULL
    BEGIN
        UPDATE dbo.InterviewerProfile
        SET    ResumeLink  = @ResumeLink,
		CodedResumeLink = null,
        ModifiedOn  = GETDATE()
        WHERE  Id = @InterviewerProfileId
    END

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO
