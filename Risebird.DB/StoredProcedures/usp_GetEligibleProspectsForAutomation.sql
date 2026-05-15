CREATE OR ALTER PROCEDURE [dbo].[usp_GetEligibleProspectsForAutomation]
    @BatchSize INT = 2
AS
BEGIN
    SET NOCOUNT ON;
	set @BatchSize = 3

    DECLARE @Today DATE = CAST(GETUTCDATE() AS DATE);

    -- Reset daily counter if last attempt was a prior day
    UPDATE NaukriProspectiveInterviewer
    SET PlivoCallAttemptsToday = 0
    WHERE PlivoCallLastAttemptDate IS NOT NULL
      AND PlivoCallLastAttemptDate < @Today
      AND PlivoCallAttemptsToday > 0;

    ;WITH Eligible AS (
        SELECT
            np.*,
            magInfo.AssessmentGroupId AS ResolvedAssessmentGroupId,
            magInfo.AssessmentGroupName,
            ROW_NUMBER() OVER (ORDER BY
                -- Priority 1: Call completed with interested disposition — ready for email
                CASE WHEN np.PlivoCallStatus IN ('completed', 'answered')
                          AND LOWER(ISNULL(np.PlivoProspectDisposition, '')) = 'interested'
                          AND ISNULL(np.InvitationEmailSent, 0) = 0
                     THEN 0 ELSE 1 END ASC,
                -- Priority 2: Call completed with non-interested disposition — needs cleanup
                CASE WHEN np.PlivoCallStatus IN ('completed', 'answered')
                          AND LOWER(ISNULL(np.PlivoProspectDisposition, '')) <> 'interested'
                     THEN 0 ELSE 1 END ASC,
                -- Then chronological
                np.ReceivedAt ASC, np.Id ASC
            ) AS RowNum
        FROM NaukriProspectiveInterviewer np
        OUTER APPLY (
            SELECT TOP 1 mag.AssessmentGroupId, ag.AssessmentGroup AS AssessmentGroupName
            FROM MasterAssessmentGroup mag
            LEFT JOIN AssessmentGroup ag ON ag.id = mag.AssessmentGroupId
            WHERE LTRIM(RTRIM(LOWER(mag.MAGName))) = LTRIM(RTRIM(LOWER(np.MagName)))
              AND (mag.IsDeleted IS NULL OR mag.IsDeleted = 0)
        ) magInfo
        WHERE np.Status IN ('Open', 'InProgress')
          AND ISNULL(np.InvitationEmailSent, 0) = 0
          AND ISNULL(np.AutomationStatus, '') NOT IN (
              'Cold', 'ManualRequired', 'EmailSent', 'Completed', 'Error',
              'ParsingResume', 'ResolvingMAG', 'SendingEmail'
          )
          AND ISNULL(np.PlivoCallStatus, '') NOT IN ('initiated', 'triggered')
          AND np.ResumeLink IS NOT NULL AND np.ResumeLink <> ''
          AND (
              -- Needs resume parse
              ISNULL(np.ResumeParseCompleted, 0) = 0
              -- OR: Resume parsed, not duplicate, needs MAG link
              OR (np.ResumeParseCompleted = 1 AND ISNULL(np.IsExistingInterviewer, 0) = 0
                  AND (np.ApplicationLink IS NULL OR np.ApplicationLink = ''))
              -- OR: MAG link generated, needs call (within limits), phone present
              OR (np.ApplicationLink IS NOT NULL AND np.ApplicationLink <> ''
                  AND ISNULL(np.PlivoCallAttemptsTotal, 0) < 5
                  AND ISNULL(np.PlivoCallAttemptsToday, 0) < 2
                  AND ISNULL(np.PlivoCallStatus, '') NOT IN ('completed', 'answered')
                  AND np.ParsedPhone IS NOT NULL AND np.ParsedPhone <> '')
              -- OR: Call completed with interested disposition, needs email
              OR (np.PlivoCallStatus IN ('completed', 'answered')
                  AND LOWER(ISNULL(np.PlivoProspectDisposition, '')) = 'interested')
              -- OR: Call completed with non-interested disposition, needs status cleanup
              OR (np.PlivoCallStatus IN ('completed', 'answered')
                  AND LOWER(ISNULL(np.PlivoProspectDisposition, '')) <> 'interested'
                  AND ISNULL(np.AutomationStatus, '') NOT IN ('Cold', 'ManualRequired'))
          )
    )
    SELECT * FROM Eligible WHERE RowNum <= @BatchSize;
END
GO
