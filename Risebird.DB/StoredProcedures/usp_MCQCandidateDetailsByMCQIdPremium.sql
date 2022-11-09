IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_MCQCandidateDetailsByMCQIdPremium'))
BEGIN
    DROP PROCEDURE dbo.usp_MCQCandidateDetailsByMCQIdPremium
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_MCQCandidateDetailsByMCQIdPremium              
@MCQPId varchar(50)
AS              
BEGIN 
Select sl.EmailAddress as CandidateEmail,sl.ClientHR,sl.ApplyingCompany,ca.TotalExprience,ca.Skills,ca.PartitionKey as Id,            
ca.CandidateScore,ca.TotalQuestions,sl.PartitionKey,sl.MCQId as SameID,ca.VideoLink,ca.Feedbackreating,ca.FeedbackComment,sl.ShareDate as Sharetime,sl.roomId,      
ms.AadhaarVerification,(IsNull(ul.FirstName,'') +SPACE(1)+ ISNULL(ul.LastName,'')) as Name,
ad.Name as AadhaarVerificationDone,sl.PartitionKey as SMCQId     
from MCQShareListPremium sl left join MCQCandidatesPremium ca on sl.EmailAddress=ca.CandidateEmail and sl.MCQID=ca.MCQId       
join MCQSets ms on ms.PartitionKey=sl.MCQId    
left join UserList ul on ul.EmailId=sl.EmailAddress  
left join AadhaarDetails ad on ad.email=sl.EmailAddress
where sl.MCQId=@MCQPId 
group by sl.EmailAddress,sl.ClientHR,sl.ApplyingCompany,ca.TotalExprience,ca.Skills,ca.PartitionKey,            
ca.CandidateScore,ca.TotalQuestions,sl.PartitionKey,sl.MCQId,ca.VideoLink,ca.Feedbackreating,ca.FeedbackComment,sl.ShareDate,sl.roomId,ms.AadhaarVerification,(IsNull(ul.FirstName,'') +SPACE(1)+ ISNULL(ul.LastName,'')),ad.Name  
END  
GO