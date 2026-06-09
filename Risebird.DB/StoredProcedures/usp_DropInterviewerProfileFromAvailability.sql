IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_DropInterviewerProfileFromAvailability')
    DROP PROCEDURE usp_DropInterviewerProfileFromAvailability
GO

CREATE PROCEDURE [dbo].[usp_DropInterviewerProfileFromAvailability]
    @InterviewerProfileId   INT,
    @ModifiedBy             VARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE InterviewerProfile
    SET Isavailable = 4,
        AvailabilityResponseDate = GETDATE(),
        ModifiedOn = GETDATE(),
        ModifiedBy = @ModifiedBy
    WHERE Id = @InterviewerProfileId

	update UserList set IsActive = 0,
		InactivationType = 'ProfileDrop',
		InactivatedBy = ISNULL(@ModifiedBy, 'Self'),
		InactivatedAt = GETDATE()
	where id = (select UserId from InterviewerProfile where id = @InterviewerProfileId)

    SELECT @@ROWCOUNT AS RowsAffected
END
GO
