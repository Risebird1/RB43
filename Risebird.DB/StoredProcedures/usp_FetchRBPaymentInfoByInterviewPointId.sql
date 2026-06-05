IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchRBPaymentInfoByInterviewPointId'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchRBPaymentInfoByInterviewPointId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_FetchRBPaymentInfoByInterviewPointId
    @InterviewPointId INT,
    @Emailid          VARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        ph.TransferUtr,
        COALESCE(ph.TransactionDate, ph.PaymentDate) AS PaymentDate,
        ph.Comment AS PaymentComment,
        ph.PayoutStatus
    FROM InterviewPoints ip
    INNER JOIN UserList ul ON ul.id = ip.userid AND LOWER(ul.EmailId) = LOWER(@Emailid)
    LEFT JOIN PaymentHistory ph ON ph.id = ip.PaymentHistoryId
    WHERE ip.id = @InterviewPointId
      AND ip.Status = 'Paid';
END
GO
