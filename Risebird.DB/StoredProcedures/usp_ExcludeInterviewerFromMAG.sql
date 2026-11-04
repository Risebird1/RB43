IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_ExcludeInterviewerFromMAG'))
BEGIN
    DROP PROCEDURE dbo.usp_ExcludeInterviewerFromMAG
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Removes an interviewer from every MAG / assessment group and skill channel.
-- Snapshots InterviewerProfile.AssessmentGroupId into InterviewerMAGExclusions (so usp_RestoreInterviewerToMAG
-- can put it back), clears it, and sets every TransInterviewer row inactive.
-- @Reason: CallOptOut | ManualInactive. Called by usp_RecordInterviewerCallOptOut, usp_InactivateInterviewer
-- (Manual* types only) and usp_UpdateInterviewerFlags (access toggled off). Auto-inactivations don't call it.
CREATE PROCEDURE [dbo].[usp_ExcludeInterviewerFromMAG]
    @UserId      int,
    @Reason      varchar(50),
    @PerformedBy varchar(200),
    @Now         datetime
AS
BEGIN
    SET NOCOUNT ON;

    IF @UserId IS NULL OR @Reason NOT IN ('CallOptOut', 'ManualInactive') RETURN

    DECLARE @Groups varchar(500)
    SELECT TOP 1 @Groups = NULLIF(LTRIM(RTRIM(ip.AssessmentGroupId)), '')
    FROM InterviewerProfile ip
    WHERE ip.UserId = @UserId
    ORDER BY ip.Id DESC

    -- Nothing to snapshot when the groups are already empty (e.g. already excluded for the other reason);
    -- write a row anyway so the restore for this reason knows the interviewer was excluded.
    INSERT INTO InterviewerMAGExclusions (UserId, RemovedAssessmentGroupIds, Reason, PerformedBy, CreatedOn)
    VALUES (@UserId, @Groups, @Reason, @PerformedBy, @Now)

    UPDATE InterviewerProfile
    SET AssessmentGroupId = NULL,
        ModifiedBy = @PerformedBy,
        ModifiedOn = @Now
    WHERE UserId = @UserId

    UPDATE TransInterviewer
    SET IsActive = 0,
        IsAvailable = 0
    WHERE UserId = @UserId
END
GO
