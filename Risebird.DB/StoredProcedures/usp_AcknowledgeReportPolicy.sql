IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AcknowledgeReportPolicy'))
BEGIN
    DROP PROCEDURE dbo.usp_AcknowledgeReportPolicy
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Interviewer report edit window: records that the signed-in interviewer accepted the report policy popup
-- on the dashboard, so it is not shown again.
CREATE PROCEDURE [dbo].[usp_AcknowledgeReportPolicy]
    @InterviewerEmail VARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE ip
       SET ReportPolicyAcknowledged = 1
      FROM dbo.InterviewerProfile ip
      JOIN dbo.UserList ul ON ul.id = ip.UserId
     WHERE LTRIM(RTRIM(ul.EmailId)) = LTRIM(RTRIM(@InterviewerEmail))
       AND ISNULL(ip.ReportPolicyAcknowledged, 0) = 0;

    SELECT @@ROWCOUNT AS UpdatedRows;
END
GO
