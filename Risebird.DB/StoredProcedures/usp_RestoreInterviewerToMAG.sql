IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_RestoreInterviewerToMAG'))
BEGIN
    DROP PROCEDURE dbo.usp_RestoreInterviewerToMAG
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Puts back the assessment groups removed by usp_ExcludeInterviewerFromMAG for @Reason only
-- (CallOptOut | ManualInactive), merged with any groups added since. Marks those exclusion rows restored.
-- When the interviewer is still excluded for the other reason, the groups are handed over to that open
-- exclusion instead of being written back, so they only return to MAG once both reasons are cleared.
-- TransInterviewer (skill channel) rows are NOT reactivated, same as usp_ReactivateInterviewer.
-- @RestoredGroups OUTPUT: the CSV written back (NULL if nothing was). No result set, so callers can EXEC it freely.
CREATE PROCEDURE [dbo].[usp_RestoreInterviewerToMAG]
    @UserId     int,
    @Reason     varchar(50),
    @RestoredBy varchar(200),
    @Now        datetime,
    @RestoredGroups varchar(max) = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @RestoredGroups = NULL

    IF NOT EXISTS (SELECT 1 FROM InterviewerMAGExclusions
                   WHERE UserId = @UserId AND Reason = @Reason AND RestoredOn IS NULL)
    BEGIN
        RETURN
    END

    DECLARE @Removed varchar(max)
    SELECT @Removed = STRING_AGG(CAST(e.RemovedAssessmentGroupIds AS varchar(max)), ',')
    FROM InterviewerMAGExclusions e
    WHERE e.UserId = @UserId AND e.Reason = @Reason AND e.RestoredOn IS NULL
      AND NULLIF(LTRIM(RTRIM(e.RemovedAssessmentGroupIds)), '') IS NOT NULL

    -- The exclusion rows are marked restored only after the groups are safely written (end of procedure), so a
    -- failed write never loses them.
    DECLARE @OtherOpenId int
    SELECT TOP 1 @OtherOpenId = e.Id
    FROM InterviewerMAGExclusions e
    WHERE e.UserId = @UserId AND e.Reason <> @Reason AND e.RestoredOn IS NULL
    ORDER BY e.Id DESC

    IF @Removed IS NOT NULL AND @OtherOpenId IS NOT NULL
    BEGIN
        -- Still excluded for the other reason: hand the groups to that exclusion's latest open row (varchar(max)).
        UPDATE e
        SET RemovedAssessmentGroupIds = (
                SELECT STRING_AGG(CAST(g.value AS varchar(max)), ',')
                FROM (SELECT DISTINCT LTRIM(RTRIM(s.value)) AS value
                      FROM STRING_SPLIT(ISNULL(e.RemovedAssessmentGroupIds, '') + ',' + @Removed, ',') s
                      WHERE LTRIM(RTRIM(s.value)) <> '') g)
        FROM InterviewerMAGExclusions e
        WHERE e.Id = @OtherOpenId
    END
    ELSE IF @Removed IS NOT NULL
    BEGIN
        -- Current groups first, then the restored ones; each id once.
        DECLARE @Merged varchar(max)
        DECLARE @Current varchar(max) = ISNULL((SELECT TOP 1 ip.AssessmentGroupId FROM InterviewerProfile ip
                                                WHERE ip.UserId = @UserId ORDER BY ip.Id DESC), '')
        -- Position of each id in its source CSV keeps the original order (STRING_SPLIT's ordinal argument needs
        -- SQL Server 2022, so it isn't used).
        SELECT @Merged = STRING_AGG(CAST(g.value AS varchar(max)), ',') WITHIN GROUP (ORDER BY g.Src, g.FirstPos)
        FROM (SELECT x.value, MIN(x.Src * 1000000 + x.Pos) / 1000000 AS Src, MIN(x.Src * 1000000 + x.Pos) AS FirstPos
              FROM (SELECT LTRIM(RTRIM(s.value)) AS value, 0 AS Src,
                           CHARINDEX(',' + LTRIM(RTRIM(s.value)) + ',', ',' + REPLACE(@Current, ' ', '') + ',') AS Pos
                    FROM STRING_SPLIT(@Current, ',') s
                    UNION ALL
                    SELECT LTRIM(RTRIM(s.value)), 1,
                           CHARINDEX(',' + LTRIM(RTRIM(s.value)) + ',', ',' + REPLACE(@Removed, ' ', '') + ',')
                    FROM STRING_SPLIT(@Removed, ',') s) x
              WHERE x.value <> ''
              GROUP BY x.value) g

        -- AssessmentGroupId is varchar(500): cut at a whole id, never mid-id, and keep what didn't fit in a new
        -- open exclusion row (same reason) so it isn't lost.
        DECLARE @Overflow varchar(max) = NULL
        IF LEN(@Merged) > 500
        BEGIN
            DECLARE @Keep int = 501 - CHARINDEX(',', REVERSE(LEFT(@Merged, 501)))
            SET @Overflow = SUBSTRING(@Merged, @Keep + 2, LEN(@Merged))
            SET @Merged = LEFT(@Merged, @Keep)
        END

        UPDATE InterviewerProfile
        SET AssessmentGroupId = @Merged,
            ModifiedBy = @RestoredBy,
            ModifiedOn = @Now
        WHERE UserId = @UserId

        SET @RestoredGroups = @Merged
    END

    UPDATE InterviewerMAGExclusions
    SET RestoredOn = @Now,
        RestoredBy = @RestoredBy
    WHERE UserId = @UserId AND Reason = @Reason AND RestoredOn IS NULL

    IF @Overflow IS NOT NULL
        INSERT INTO InterviewerMAGExclusions (UserId, RemovedAssessmentGroupIds, Reason, PerformedBy, CreatedOn)
        VALUES (@UserId, @Overflow, @Reason, 'usp_RestoreInterviewerToMAG (did not fit AssessmentGroupId)', @Now)
END
GO
