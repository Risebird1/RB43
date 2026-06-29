CREATE OR ALTER PROCEDURE [dbo].[usp_FetchInterviewerChallanNumbers]
    @FinancialYear NVARCHAR(9),
    @Quarter       NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    -- Returns one row per interviewer-challan link for the quarter.
    -- Service layer aggregates ChallanSerialNumber per Userid.
    SELECT
        tci.Userid,
        tci.PAN,
        tc.ChallanSerialNumber,
        tc.ChallanReferenceNumber
    FROM TdsChallanInterviewer tci
    JOIN TdsChallan tc ON tc.id = tci.ChallanId
    WHERE tc.FinancialYear = @FinancialYear
      AND tc.Quarter = @Quarter
    ORDER BY tci.Userid, tc.WeekNumber;
END
GO
