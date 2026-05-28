CREATE OR ALTER PROCEDURE [dbo].[usp_GetProspectCallOutcomesForMAG]
    @MagName NVARCHAR(200),
    @Page INT = 1,
    @PageSize INT = 25,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @Page < 1 SET @Page = 1;
    IF @PageSize < 1 SET @PageSize = 25;
    IF @PageSize > 200 SET @PageSize = 200;

    SELECT @TotalCount = COUNT(*)
    FROM NaukriProspectiveInterviewer np
    WHERE np.MagName = @MagName
      AND np.ParsedPhone IS NOT NULL
      AND LEN(LTRIM(RTRIM(np.ParsedPhone))) >= 10;

    SELECT
        np.Id,
        np.CandidateName,
        np.ParsedEmail,
        np.ParsedPhone,
        np.ExperienceYears,
        np.CurrentRole,
        np.Location,
        np.KeySkills,
        np.PlivoCallStatus,
        np.PlivoConversationId,
        np.PlivoCallAt,
        np.PlivoCallDuration,
        np.PlivoProspectDisposition,
        np.PlivoOnboardingPreference,
        np.PlivoKycAccepted,
        np.PlivoCalledBy,
        np.PlivoCallSummary,
        np.PlivoCallAttemptsTotal,
        np.InvitationEmailSent,
        np.Status,
        np.Source
    FROM NaukriProspectiveInterviewer np
    WHERE np.MagName = @MagName
      AND np.ParsedPhone IS NOT NULL
      AND LEN(LTRIM(RTRIM(np.ParsedPhone))) >= 10
    ORDER BY
        CASE WHEN np.PlivoCallStatus IS NOT NULL THEN 0 ELSE 1 END ASC,
        np.PlivoCallAt DESC,
        np.ReceivedAt ASC
    OFFSET (@Page - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO
