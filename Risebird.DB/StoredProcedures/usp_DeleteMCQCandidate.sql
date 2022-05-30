IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_DeleteMCQCandidate'))
BEGIN
    DROP PROCEDURE dbo.usp_DeleteMCQCandidate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_DeleteMCQCandidate    
@SharePartitionKey varchar(50),    
@CandidatePartitionKey varchar(50)    
AS    
BEGIN    
DELETE From MCQCandidateQuestionAnswer where CandidateID=@CandidatePartitionKey    
DELETE From MCQCandidates where PartitionKey=@CandidatePartitionKey    
DELETE From MCQShareList where PartitionKey=@SharePartitionKey    
END
GO