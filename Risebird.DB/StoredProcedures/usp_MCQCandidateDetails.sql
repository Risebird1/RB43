IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_MCQCandidateDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_MCQCandidateDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_MCQCandidateDetails
AS        
BEGIN        
Select sl.EmailAddress as CandidateEmail,sl.ClientHR,sl.ApplyingCompany,ca.TotalExprience,ca.Skills,ca.PartitionKey as Id,      
ca.CandidateScore,ca.TotalQuestions,sl.PartitionKey,sl.MCQId as SameID,ca.VideoLink,ca.Feedbackreating,ca.FeedbackComment,sl.ShareDate as Sharetime,sl.roomId,
Case When Sum(Case When mts.RequiredLevelId<=mts.LevelId then 100 when mts.RequiredLevelId>=mts.LevelId then (100-((mts.RequiredLevelId-mts.LevelId)*100/mts.RequiredLevelId)) else 0 end)>0 then Sum(Case When mts.RequiredLevelId<=mts.LevelId then 100 when mts.RequiredLevelId>=mts.LevelId then (100-((mts.RequiredLevelId-mts.LevelId)*100/mts.RequiredLevelId)) else 0 end)/Count(mts.id) else 0 end as Selectability,
Case When Sum(Case When mtj.RequiredLevelId<=mtj.LevelId then 100 when mtj.RequiredLevelId>=mtj.LevelId then (100-((mtj.RequiredLevelId-mtj.LevelId)*100/mtj.RequiredLevelId)) else 0 end)>0 then Sum(Case When mtj.RequiredLevelId<=mtj.LevelId then 100 when mtj.RequiredLevelId>=mtj.LevelId then (100-((mtj.RequiredLevelId-mtj.LevelId)*100/mtj.RequiredLevelId)) else 0 end)/Count(mtj.id) else 0 end as Joinability,ms.AadhaarVerification
from MCQShareList sl left join MCQCandidates ca on sl.EmailAddress=ca.CandidateEmail and sl.MCQID=ca.MCQId 
left join MCQ_TransSelectability mts on mts.MCQID=sl.MCQID and mts.CandidateEmail=sl.EmailAddress
left join MCQ_TransJoinability mtj on mtj.MCQID=sl.MCQID and mtj.CandidateEmail=sl.EmailAddress
join MCQSets ms on ms.PartitionKey=sl.MCQId
group by sl.EmailAddress,sl.ClientHR,sl.ApplyingCompany,ca.TotalExprience,ca.Skills,ca.PartitionKey,      
ca.CandidateScore,ca.TotalQuestions,sl.PartitionKey,sl.MCQId,ca.VideoLink,ca.Feedbackreating,ca.FeedbackComment,sl.ShareDate,sl.roomId,ms.AadhaarVerification
END 
Go