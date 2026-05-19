IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_GetProspectCallOutcomesForMAG')
    DROP PROCEDURE usp_GetProspectCallOutcomesForMAG
GO

CREATE PROCEDURE [dbo].[usp_GetProspectCallOutcomesForMAG]
    @MagName NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

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
END
GO
