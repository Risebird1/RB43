IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviewerBankDetailsByUserId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerBankDetailsByUserId
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE dbo.usp_GetInterviewerBankDetailsByUserId
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ul.id AS Userid,
        (ul.FirstName + ul.LastName) AS FirstName,
        ul.EmailId AS InterviewerEmailid,
        ul.MobileNumber,
        tic.AccountName,
        tic.AccountNumber,
        tic.IFSCCode,
        tic.PAN,
        ISNULL(tic.IsBankVerified, 0) AS IsBankVerified
    FROM UserList ul
    INNER JOIN InterviewerProfile irp ON irp.UserId = ul.id
    LEFT JOIN TransInterviewerAccDetails tic ON tic.InterviewerProfileId = irp.id
    WHERE ul.id = @UserId;
END
GO
