CREATE OR ALTER PROCEDURE [dbo].[usp_GetProfileInterviewerContact]
    @InterviewerProfileId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Contact details for an active interviewer profile, used to place an
    -- availability call without first creating an InterviewerEmailAvailability row.
    SELECT TOP 1
        ip.Id            AS InterviewerProfileId,
        ip.UserId        AS UserId,
        ip.NickName      AS InterviewerName,
        ul.MobileNumber  AS Phone,
        ul.MobileNumber  AS UserMobileNumber,
        ul.EmailId       AS SenderEmail
    FROM InterviewerProfile ip
    INNER JOIN UserList ul ON ul.Id = ip.UserId
    WHERE ip.Id = @InterviewerProfileId
      AND ISNULL(ip.IsDeleted, 0) = 0
      AND ISNULL(ip.Isavailable, 0) <> 4
      AND ul.IsActive = 1;
END
GO
