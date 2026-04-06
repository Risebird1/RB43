IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_SaveAiContradictionDetails]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_SaveAiContradictionDetails]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_SaveAiContradictionDetails]  
    @qcEvaluationId INT,  
    @contradictionJson varchar(max)  
AS  
BEGIN  
Declare @Count int=0
Set @Count=(Select Count(*) from QcInterviewAIevaluation where interviewid=@qcEvaluationId)

if(@Count>0)
begin
        UPDATE [dbo].[QcInterviewAIevaluation]  
        SET [ContradictionJson]=@contradictionJson  
        WHERE [interviewid] = @qcEvaluationId;  
end
else
begin
Insert into QcInterviewAIevaluation (InterviewId,ContradictionJson) values(@qcEvaluationId,@contradictionJson)
end
END 
GO
