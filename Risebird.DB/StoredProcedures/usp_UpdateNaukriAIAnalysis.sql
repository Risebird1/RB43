IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_UpdateNaukriAIAnalysis')
    DROP PROCEDURE usp_UpdateNaukriAIAnalysis
GO

CREATE PROCEDURE [dbo].[usp_UpdateNaukriAIAnalysis]
    @Id                         INT,
    @ResumeAIAnalysisCompleted  BIT             = NULL,
    @ResumeAIAnalysisResult     NVARCHAR(MAX)   = NULL,
    @AIVerdict                  VARCHAR(50)     = NULL,
    @AIFitmentScore             INT             = NULL,
    @NewStatus                  VARCHAR(50)     = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.NaukriProspectiveInterviewer
    SET    ResumeAIAnalysisCompleted = @ResumeAIAnalysisCompleted,
           ResumeAIAnalysisResult    = @ResumeAIAnalysisResult,
           AIVerdict                 = @AIVerdict,
           AIFitmentScore            = @AIFitmentScore,
           Status                    = ISNULL(@NewStatus, Status),
           UpdatedAt                 = GETUTCDATE()
    WHERE  Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO
