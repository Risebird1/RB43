CREATE OR ALTER PROCEDURE [dbo].[usp_AddOrUpdateTdsReturnFiling]
    @FinancialYear          NVARCHAR(9),
    @Quarter                NVARCHAR(20),
    @ReturnType             NVARCHAR(10),
    @FilingDate             DATE,
    @AcknowledgementNumber  NVARCHAR(50),
    @TokenNumber            NVARCHAR(50),
    @Status                 NVARCHAR(20),
    @Remarks                NVARCHAR(500),
    @Operator               NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM TdsReturnFiling WHERE FinancialYear = @FinancialYear AND Quarter = @Quarter)
    BEGIN
        UPDATE TdsReturnFiling SET
            ReturnType            = @ReturnType,
            FilingDate            = @FilingDate,
            AcknowledgementNumber = @AcknowledgementNumber,
            TokenNumber           = @TokenNumber,
            Status                = @Status,
            Remarks               = @Remarks,
            UpdatedOn             = GETDATE(),
            UpdatedBy             = @Operator
        WHERE FinancialYear = @FinancialYear AND Quarter = @Quarter;
    END
    ELSE
    BEGIN
        INSERT INTO TdsReturnFiling
            (FinancialYear, Quarter, ReturnType, FilingDate, AcknowledgementNumber,
             TokenNumber, Status, Remarks, CreatedOn, CreatedBy)
        VALUES
            (@FinancialYear, @Quarter, @ReturnType, @FilingDate, @AcknowledgementNumber,
             @TokenNumber, @Status, @Remarks, GETDATE(), @Operator);
    END
END
GO
