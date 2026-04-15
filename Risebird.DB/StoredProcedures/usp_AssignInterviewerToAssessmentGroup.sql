IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_AssignInterviewerToAssessmentGroup')
    DROP PROCEDURE usp_AssignInterviewerToAssessmentGroup
GO

CREATE PROCEDURE [dbo].[usp_AssignInterviewerToAssessmentGroup]
    @EmailAvailabilityId    INT,
    @InterviewerProfileId   INT,
    @AssessmentGroupId      INT,
    @AssignedBy             VARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CurrentAGIds VARCHAR(500)
    SELECT @CurrentAGIds = ISNULL(AssessmentGroupId, '') FROM InterviewerProfile WHERE Id = @InterviewerProfileId

    -- Use string_split for accurate matching (CHARINDEX can false-positive, e.g. '1' in '12')
    IF NOT EXISTS (
        SELECT 1 FROM string_split(@CurrentAGIds, ',')
        WHERE LTRIM(RTRIM(value)) = CAST(@AssessmentGroupId AS VARCHAR)
    )
    BEGIN
        IF @CurrentAGIds = '' OR @CurrentAGIds IS NULL
            SET @CurrentAGIds = CAST(@AssessmentGroupId AS VARCHAR)
        ELSE
            SET @CurrentAGIds = @CurrentAGIds + ',' + CAST(@AssessmentGroupId AS VARCHAR)

        UPDATE InterviewerProfile
        SET AssessmentGroupId = @CurrentAGIds,
            ModifiedOn = GETDATE(),
            ModifiedBy = @AssignedBy
        WHERE Id = @InterviewerProfileId
    END

    IF NOT EXISTS (
        SELECT 1 FROM AssessmentGroupInterviewers
        WHERE AssessmentGroupId = @AssessmentGroupId AND InterviewerProfileId = @InterviewerProfileId
    )
    BEGIN
        INSERT INTO AssessmentGroupInterviewers (AssessmentGroupId, InterviewerProfileId, IsSME, CreatedBy, CreatedOn)
        VALUES (@AssessmentGroupId, @InterviewerProfileId, 0, ISNULL(@AssignedBy, 'System'), GETDATE())
    END

    UPDATE InterviewerEmailAvailability
    SET AssessmentGroupId          = @AssessmentGroupId,
        AssessmentGroupAssignedAt  = GETUTCDATE(),
        Status                     = 'Closed',
        ProcessedBy                = @AssignedBy,
        UpdatedAt                  = GETUTCDATE()
    WHERE Id = @EmailAvailabilityId

    SELECT 1 AS Success
END
GO
