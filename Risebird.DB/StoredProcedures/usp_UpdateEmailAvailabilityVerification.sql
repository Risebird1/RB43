CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateEmailAvailabilityVerification]
    @Id                         INT,
    @EmploymentVerified         BIT            = NULL,
    @ResumeAIAnalysisCompleted  BIT            = NULL,
    @ResumeAIAnalysisResult     NVARCHAR(MAX)  = NULL,
    @NewStatus                  VARCHAR(50)    = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.InterviewerEmailAvailability
    SET    EmploymentVerified         = ISNULL(@EmploymentVerified, EmploymentVerified),
           ResumeAIAnalysisCompleted  = ISNULL(@ResumeAIAnalysisCompleted, ResumeAIAnalysisCompleted),
           ResumeAIAnalysisResult     = ISNULL(@ResumeAIAnalysisResult, ResumeAIAnalysisResult),
           Status                     = ISNULL(@NewStatus, Status),
           UpdatedAt                  = GETUTCDATE()
    WHERE  Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO
