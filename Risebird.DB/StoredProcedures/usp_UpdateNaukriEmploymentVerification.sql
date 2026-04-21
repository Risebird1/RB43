IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_UpdateNaukriEmploymentVerification')
    DROP PROCEDURE usp_UpdateNaukriEmploymentVerification
GO

CREATE PROCEDURE [dbo].[usp_UpdateNaukriEmploymentVerification]
    @Id                     INT,
    @EmploymentVerified     BIT             = NULL,
    @EmploymentCompanyName  NVARCHAR(300)   = NULL,
    @EmploymentIsEmployed   BIT             = NULL,
    @EmploymentDetails      NVARCHAR(MAX)   = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.NaukriProspectiveInterviewer
    SET    EmploymentVerified      = @EmploymentVerified,
           EmploymentCompanyName   = @EmploymentCompanyName,
           EmploymentIsEmployed    = @EmploymentIsEmployed,
           EmploymentDetails       = @EmploymentDetails,
           UpdatedAt               = GETUTCDATE()
    WHERE  Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO
