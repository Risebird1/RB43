IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_SaveQcInterviewAIMergedDetails]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_SaveQcInterviewAIMergedDetails]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_SaveQcInterviewAIMergedDetails]
    @qcEvaluationId INT,
    @mergedJson NVARCHAR(MAX),
    @totalPenalty INT = NULL,
    @interviewerSuspicious VARCHAR(50) = NULL,
    @redoInterview BIT = NULL,
    @contradictionJson varchar(max)
AS
BEGIN
        UPDATE [dbo].[QcInterviewAIevaluation]
        SET [MergedAIQCDetails] = @mergedJson,
            [AIMergeTotalPenalty] = @totalPenalty,
            [AIMergeInterviewerSuspicious] = @interviewerSuspicious,
            [AIMergeRedoInterview] = @redoInterview,
            [QCDone] = 1,
            [QCDoneOn] = DATEADD(MINUTE, 330, GETDATE()),
			[ContradictionJson]=@contradictionJson
        WHERE [id] = @qcEvaluationId;
END


GO
