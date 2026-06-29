CREATE OR ALTER PROCEDURE [dbo].[usp_FetchTdsReturnFiling]
    @FinancialYear NVARCHAR(9),
    @Quarter       NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        id, FinancialYear, Quarter, ReturnType, FilingDate,
        AcknowledgementNumber, TokenNumber, Status, Remarks,
        CreatedOn, CreatedBy, UpdatedOn, UpdatedBy
    FROM TdsReturnFiling
    WHERE FinancialYear = @FinancialYear
      AND Quarter = @Quarter;
END
GO
