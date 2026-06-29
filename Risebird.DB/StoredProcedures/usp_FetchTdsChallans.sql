CREATE OR ALTER PROCEDURE [dbo].[usp_FetchTdsChallans]
    @FinancialYear NVARCHAR(9),
    @Quarter       NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        tc.id,
        tc.FinancialYear,
        tc.Quarter,
        tc.WeekNumber,
        tc.SrNo,
        tc.SectionCode,
        tc.TANNumber,
        tc.Deductor,
        tc.TdsAmount,
        tc.BSRCode,
        tc.DepositDate,
        tc.ChallanSerialNumber,
        tc.MinorHead,
        tc.ChallanReferenceNumber,
        tc.ChallanPdfPath,
        tc.PayoutReference,
        tc.PaymentDate,
        tc.IsPaid,
        tc.CreatedOn,
        tc.CreatedBy,
        tc.UpdatedOn,
        tc.UpdatedBy
    FROM TdsChallan tc
    WHERE tc.FinancialYear = @FinancialYear
      AND tc.Quarter = @Quarter
    ORDER BY tc.WeekNumber, tc.SrNo;
END
GO
