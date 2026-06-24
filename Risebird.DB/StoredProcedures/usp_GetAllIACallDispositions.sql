CREATE OR ALTER PROCEDURE [dbo].[usp_GetAllIACallDispositions]
    @Page       INT = 1,
    @PageSize   INT = 50,
    @MagName    NVARCHAR(200) = NULL,
    @Intent     VARCHAR(50)   = NULL,
    @CallStatus VARCHAR(50)   = NULL,
    @DateFrom   DATETIME      = NULL,
    @DateTo     DATETIME      = NULL
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Filtered AS (
        SELECT
            ea.Id,
            ea.InterviewerProfileId,
            ea.InterviewerName,
            ea.SenderEmail,
            ea.Phone,
            ea.MagName,
            ea.Status        AS RecordStatus,
            ea.PlivoCallStatus,
            ea.PlivoConversationId,
            ea.PlivoCallAt,
            ea.PlivoCallDuration,
            ea.PlivoCallTranscript,
            ea.PlivoCallSummary,
            ea.PlivoIntent,
            ea.PlivoAssignmentHelp,
            ea.PlivoEmailReceived,
            ea.PlivoCallbackRequested,
            ea.PlivoNotes,
            ea.PlivoCalledBy,
            ip.Experience,
            ip.NickName AS CurrentRole,
            ISNULL(ul.MobileNumber, ea.Phone) AS ResolvedPhone
        FROM InterviewerEmailAvailability ea
        LEFT JOIN InterviewerProfile ip ON ip.Id = ea.InterviewerProfileId
        LEFT JOIN UserList ul ON ul.Id = ea.UserId
        WHERE ISNULL(ea.PlivoCallStatus, '') <> ''
          AND (@MagName IS NULL OR @MagName = '' OR ea.MagName = @MagName)
          AND (@Intent IS NULL OR @Intent = '' OR ea.PlivoIntent = @Intent)
          AND (@CallStatus IS NULL OR @CallStatus = '' OR ea.PlivoCallStatus = @CallStatus)
          AND (@DateFrom IS NULL OR ea.PlivoCallAt >= @DateFrom)
          AND (@DateTo IS NULL OR ea.PlivoCallAt < DATEADD(DAY, 1, @DateTo))
    )

    SELECT
        (SELECT COUNT(*) FROM Filtered) AS TotalCount,
        f.*
    FROM Filtered f
    ORDER BY f.PlivoCallAt DESC
    OFFSET (@Page - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO
