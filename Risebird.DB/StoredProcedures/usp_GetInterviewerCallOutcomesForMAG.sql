CREATE OR ALTER PROCEDURE [dbo].[usp_GetInterviewerCallOutcomesForMAG]
    @MagName NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ea.Id, ea.InterviewerName, ea.SenderEmail,
           COALESCE(ul.MobileNumber, ea.Phone) AS Phone,
           ip.Experience, ip.CurrentRole,
           ea.PlivoCallStatus, ea.PlivoIntent, ea.PlivoAssignmentHelp,
           ea.PlivoEmailReceived, ea.PlivoCallbackRequested,
           ea.PlivoCallDuration, ea.PlivoCalledBy, ea.PlivoCallSummary, ea.PlivoNotes,
           ea.PlivoCallAt, ea.Status
    FROM InterviewerEmailAvailability ea
    LEFT JOIN UserList ul ON ea.UserId = ul.Id
    LEFT JOIN InterviewerProfile ip ON ea.InterviewerProfileId = ip.Id
    WHERE ea.MagName = @MagName
      AND ea.Status IN ('Open', 'InProgress', 'Verified')
      AND LEN(ISNULL(COALESCE(ul.MobileNumber, ea.Phone), '')) >= 10
    ORDER BY ea.PlivoCallAt DESC, ea.Id DESC
END
GO
