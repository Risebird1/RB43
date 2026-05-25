CREATE OR ALTER PROCEDURE [dbo].[usp_GetInterviewersForMAGBatchCall]
    @MagName      NVARCHAR(200),
    @OnlyUncalled BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ea.Id, ea.InterviewerName, ea.SenderEmail,
           COALESCE(ul.MobileNumber, ea.Phone) AS Phone,
           ip.Experience AS ExperienceYears,
           ea.PlivoCallStatus
    FROM InterviewerEmailAvailability ea
    LEFT JOIN UserList ul ON ea.UserId = ul.Id
    LEFT JOIN InterviewerProfile ip ON ea.InterviewerProfileId = ip.Id
    WHERE ea.MagName = @MagName
      AND ea.Status IN ('Open', 'InProgress', 'Verified')
      AND LEN(ISNULL(COALESCE(ul.MobileNumber, ea.Phone), '')) >= 10
      AND (@OnlyUncalled = 0 OR ISNULL(ea.PlivoCallStatus, '') = '')
    ORDER BY ea.ReceivedAt ASC
END
GO
