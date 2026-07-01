CREATE OR ALTER PROCEDURE [dbo].[usp_FetchInterviewerPaymentItems]
    @StartDate DATE,
    @EndDate   DATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Returns individual payment rows per interviewer for the quarter date range.
    -- Used to populate the payment history popover on the Date Paid column.
    SELECT
        ph.Userid,
        ph.PaymentDate,
        ph.TotalAmount,
        ISNULL(ph.Comment, '') AS Comment
    FROM PaymentHistory ph
    WHERE
        ph.PaymentDate >= @StartDate
        AND ph.PaymentDate <= @EndDate
        AND ph.TotalAmount > 0
        AND (ph.Status = 'Done' OR ph.PayoutStatus = 'TRANSFER_SUCCESS')
    ORDER BY ph.Userid, ph.PaymentDate;
END
GO
