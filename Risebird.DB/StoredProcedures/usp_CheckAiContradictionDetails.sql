IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_CheckAiContradictionDetails]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_CheckAiContradictionDetails]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_CheckAiContradictionDetails]  
@qcEvaluationId INT
AS  
BEGIN  
Select qai.ContradictionJson From QcInterviewAIevaluation qai where qai.InterviewId=@qcEvaluationId
END 
Go