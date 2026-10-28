IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveOperationalCostFactor'))
    DROP PROCEDURE dbo.usp_SaveOperationalCostFactor
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Adds a cost factor (@Id = 0) or updates one, returning its Id. Validation of Basis, Currency,
-- Amount and the date range is enforced by the table's CHECK constraints.
CREATE PROCEDURE usp_SaveOperationalCostFactor
    @Id            INT,
    @Name          VARCHAR(100),
    @Category      VARCHAR(50),
    @Amount        DECIMAL(18, 2),
    @Currency      VARCHAR(3),
    @Basis         VARCHAR(20),
    @EffectiveFrom DATE,
    @EffectiveTo   DATE = NULL,
    @IsActive      BIT,
    @Notes         VARCHAR(500) = NULL,
    @ChangedBy     VARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    IF ISNULL(@Id, 0) = 0
    BEGIN
        INSERT INTO OperationalCostFactor
            (Name, Category, Amount, Currency, Basis, EffectiveFrom, EffectiveTo, IsActive, Notes, CreatedBy, CreatedOn)
        VALUES
            (@Name, @Category, @Amount, @Currency, @Basis, @EffectiveFrom, @EffectiveTo, @IsActive, @Notes, @ChangedBy, GETDATE());

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
        RETURN;
    END

    UPDATE OperationalCostFactor
    SET Name          = @Name,
        Category      = @Category,
        Amount        = @Amount,
        Currency      = @Currency,
        Basis         = @Basis,
        EffectiveFrom = @EffectiveFrom,
        EffectiveTo   = @EffectiveTo,
        IsActive      = @IsActive,
        Notes         = @Notes,
        ModifiedBy    = @ChangedBy,
        ModifiedOn    = GETDATE()
    WHERE Id = @Id;

    SELECT CASE WHEN @@ROWCOUNT = 1 THEN @Id ELSE 0 END AS Id;
END
GO
