IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetRecruiters'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRecruiters
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetRecruiters]
AS
BEGIN
    SELECT Recruiter.*, ul.FirstName, ul.LastName, ul.EmailId, ul.MobileNumber
    FROM Recruiter
    JOIN UserList ul ON Recruiter.UserId = ul.id
END
GO
