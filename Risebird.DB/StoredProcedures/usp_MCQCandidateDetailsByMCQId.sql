IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_MCQCandidateDetailsByMCQId'))
BEGIN
    DROP PROCEDURE dbo.usp_MCQCandidateDetailsByMCQId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_MCQCandidateDetailsByMCQId      
@MCQPId varchar(50)      
AS      
BEGIN      
Select sl.EmailAddress as CandidateEmail,sl.ClientHR,sl.ApplyingCompany,ca.TotalExprience,ca.Skills,ca.PartitionKey as Id,    
ca.CandidateScore,ca.TotalQuestions,sl.PartitionKey,sl.MCQId as SameID,ca.VideoLink,ca.Feedbackreating,ca.FeedbackComment,sl.ShareDate as Sharetime,sl.roomId    
from MCQShareList sl left join MCQCandidates ca on sl.EmailAddress=ca.CandidateEmail and sl.MCQID=ca.MCQId where sl.MCQId=@MCQPId    
END 
GO