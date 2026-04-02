IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_SaveAiContradictionDetails]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_SaveAiContradictionDetails]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_SaveAiContradictionDetails]
    @qcEvaluationId INT,
    @contradictionJson varchar(max)
AS
BEGIN
        UPDATE [dbo].[QcInterviewAIevaluation]
        SET [ContradictionJson]=@contradictionJson
        WHERE [id] = @qcEvaluationId;
END
GO
