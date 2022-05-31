IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_DeleteMCQSetDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_DeleteMCQSetDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_DeleteMCQSetDetails    
@MCQPId varchar(50)    
AS    
BEGIN    
DELETE From MCQCandidateQuestionAnswer where MCQId=@MCQPId    
DELETE From MCQCandidates where MCQId=@MCQPId    
DELETE From MCQShareList where MCQId=@MCQPId    
DELETE From MCQquestions where MCQId=@MCQPId    
DELETE From MCQSets where PartitionKey=@MCQPId    
END
Go