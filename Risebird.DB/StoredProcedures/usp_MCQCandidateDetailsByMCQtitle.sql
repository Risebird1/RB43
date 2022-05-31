  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_MCQCandidateDetailsByMCQtitle'))
BEGIN
    DROP PROCEDURE dbo.usp_MCQCandidateDetailsByMCQtitle
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_MCQCandidateDetailsByMCQtitle      
@MCQtitle varchar(50)      
AS      
BEGIN      
Select sl.EmailAddress as CandidateEmail,sl.ClientHR,sl.ApplyingCompany,ca.TotalExprience,ca.Skills,ca.PartitionKey as Id,    
ca.CandidateScore,ca.TotalQuestions,sl.PartitionKey,sl.MCQId as SameID,ca.VideoLink,ca.Feedbackreating,ca.FeedbackComment,sl.ShareDate as Sharetime    
from MCQSet ms join MCQShareList sl on ms.PartitionKey=sl.MCQId left join MCQCandidates ca on sl.EmailAddress=ca.CandidateEmail and sl.MCQID=ca.MCQId 
where ms.Title=@MCQtitle    
END 
GO