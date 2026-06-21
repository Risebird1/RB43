IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetRecruiterActiveStatusByInterviewId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRecruiterActiveStatusByInterviewId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetRecruiterActiveStatusByInterviewId]
    @interviewid INT
AS
BEGIN
    SELECT ISNULL(tc.IsActive, 1) AS IsActive
    FROM interviewmeetings im
    JOIN IRF ON IRF.IRFId = im.IRFId
    LEFT JOIN UserList ul ON ul.EmailId = im.ClientHrContact
    LEFT JOIN TransClientUser tc ON tc.ClientUserId = ul.id AND tc.IRFId = IRF.id
    WHERE im.id = @interviewid
END
GO
