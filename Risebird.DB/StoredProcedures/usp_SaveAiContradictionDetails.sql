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
Declare @Count int=0,@ContradictionScore varchar(10),@ContradictionVerdict varchar(100),@ContradictionDirectionVerdict varchar(100)
Set @Count=(Select Count(*) from QcInterviewAIevaluation where interviewid=@qcEvaluationId)  
    
	IF ISJSON(@contradictionJson) = 1
    BEGIN
        SET @ContradictionScore = JSON_VALUE(@contradictionJson, '$.OverallContradictionScore');
        SET @ContradictionVerdict = JSON_VALUE(@contradictionJson, '$.OverallContradictionVerdict');
        SET @ContradictionDirectionVerdict = JSON_VALUE(@contradictionJson, '$.OverallContradictionDirectionVerdict');
    END
 
if(@Count>0)  
begin  
        UPDATE [dbo].[QcInterviewAIevaluation]    
        SET [ContradictionJson]=@contradictionJson,  
        [ContradictionScore]=@ContradictionScore,
        [ContradictionVerdict]=@ContradictionVerdict,
        [ContradictionDirectionVerdict]=@ContradictionDirectionVerdict
        WHERE [interviewid] = @qcEvaluationId;    
end  
else  
begin  
Insert into QcInterviewAIevaluation (InterviewId,ContradictionJson,ContradictionScore,ContradictionVerdict,ContradictionDirectionVerdict) values(@qcEvaluationId,@contradictionJson,@ContradictionScore,@ContradictionVerdict,@ContradictionDirectionVerdict)
end  
END  
GO
