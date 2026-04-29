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
Declare @Count int=0,@ContradictionScore varchar(10)  
Set @Count=(Select Count(*) from QcInterviewAIevaluation where interviewid=@qcEvaluationId)  
Set @ContradictionScore=(CASE WHEN ISJSON(@contradictionJson) = 1 THEN JSON_VALUE(@contradictionJson, '$.OverallContradictionScore') ELSE NULL END)  
  
if(@Count>0)  
begin  
        UPDATE [dbo].[QcInterviewAIevaluation]    
        SET [ContradictionJson]=@contradictionJson,  
        [ContradictionScore]=@ContradictionScore  
        WHERE [interviewid] = @qcEvaluationId;    
end  
else  
begin  
Insert into QcInterviewAIevaluation (InterviewId,ContradictionJson,ContradictionScore) values(@qcEvaluationId,@contradictionJson,@ContradictionScore)  
end  
END  
GO
