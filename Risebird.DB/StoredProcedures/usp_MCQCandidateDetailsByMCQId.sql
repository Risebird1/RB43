IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_MCQCandidateDetailsByMCQId'))
BEGIN
    DROP PROCEDURE dbo.usp_MCQCandidateDetailsByMCQId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_MCQCandidateDetailsByMCQId              
@MCQPId varchar(50),
@requesttype bit
AS              
BEGIN 
if(@requesttype=1)
begin
Select sl.EmailAddress as CandidateEmail,sl.ClientHR,sl.ApplyingCompany,ca.TotalExprience,ca.Skills,ca.PartitionKey as Id,            
ca.CandidateScore,ca.TotalQuestions,sl.PartitionKey,sl.MCQId as SameID,ca.VideoLink,ca.Feedbackreating,ca.FeedbackComment,sl.ShareDate as Sharetime,sl.roomId,ca.RecruiterComment,      
Case When Sum(Case When mts.RequiredLevelId<=mts.LevelId then 100 when mts.RequiredLevelId>=mts.LevelId then (100-((mts.RequiredLevelId-mts.LevelId)*100/mts.RequiredLevelId)) else 0 end)>0 then Sum(Case When mts.RequiredLevelId<=mts.LevelId then 100 when   
mts.RequiredLevelId>=mts.LevelId then (100-((mts.RequiredLevelId-mts.LevelId)*100/mts.RequiredLevelId)) else 0 end)/Count(mts.id) else 0 end as Selectability,      
Case When Sum(Case When mtj.RequiredLevelId<=mtj.LevelId then 100 when mtj.RequiredLevelId>=mtj.LevelId then (100-((mtj.RequiredLevelId-mtj.LevelId)*100/mtj.RequiredLevelId)) else 0 end)>0 then Sum(Case When mtj.RequiredLevelId<=mtj.LevelId then 100 when 
mtj.RequiredLevelId>=mtj.LevelId then (100-((mtj.RequiredLevelId-mtj.LevelId)*100/mtj.RequiredLevelId)) else 0 end)/Count(mtj.id) else 0 end as Joinability,ms.AadhaarVerification,(IsNull(ul.FirstName,'') +SPACE(1)+ ISNULL(ul.LastName,'')) as Name,
ad.Name as AadhaarVerificationDone,sl.PartitionKey as SMCQId     
from MCQShareList sl left join MCQCandidates ca on sl.EmailAddress=ca.CandidateEmail and sl.MCQID=ca.MCQId       
left join MCQ_TransSelectability mts on mts.MCQID=sl.MCQID and mts.CandidateEmail=sl.EmailAddress      
left join MCQ_TransJoinability mtj on mtj.MCQID=sl.MCQID and mtj.CandidateEmail=sl.EmailAddress      
join MCQSets ms on ms.PartitionKey=sl.MCQId    
left join UserList ul on ul.EmailId=sl.EmailAddress  
left join AadhaarDetails ad on ad.email=sl.EmailAddress
where sl.MCQId=@MCQPId and ISNULL(sl.ShareType,0) =1       
group by sl.EmailAddress,sl.ClientHR,sl.ApplyingCompany,ca.TotalExprience,ca.Skills,ca.PartitionKey,            
ca.CandidateScore,ca.TotalQuestions,sl.PartitionKey,sl.MCQId,ca.VideoLink,ca.Feedbackreating,ca.FeedbackComment,sl.ShareDate,sl.roomId,ca.RecruiterComment,ms.AadhaarVerification,(IsNull(ul.FirstName,'') +SPACE(1)+ ISNULL(ul.LastName,'')),ad.Name  
end
else
begin
Select sl.EmailAddress as CandidateEmail,sl.ClientHR,sl.ApplyingCompany,ca.TotalExprience,ca.Skills,ca.PartitionKey as Id,            
ca.CandidateScore,ca.TotalQuestions,sl.PartitionKey,sl.MCQId as SameID,ca.VideoLink,ca.Feedbackreating,ca.FeedbackComment,sl.ShareDate as Sharetime,sl.roomId,ca.RecruiterComment,      
Case When Sum(Case When mts.RequiredLevelId<=mts.LevelId then 100 when mts.RequiredLevelId>=mts.LevelId then (100-((mts.RequiredLevelId-mts.LevelId)*100/mts.RequiredLevelId)) else 0 end)>0 then Sum(Case When mts.RequiredLevelId<=mts.LevelId then 100 when   
mts.RequiredLevelId>=mts.LevelId then (100-((mts.RequiredLevelId-mts.LevelId)*100/mts.RequiredLevelId)) else 0 end)/Count(mts.id) else 0 end as Selectability,      
Case When Sum(Case When mtj.RequiredLevelId<=mtj.LevelId then 100 when mtj.RequiredLevelId>=mtj.LevelId then (100-((mtj.RequiredLevelId-mtj.LevelId)*100/mtj.RequiredLevelId)) else 0 end)>0 then Sum(Case When mtj.RequiredLevelId<=mtj.LevelId then 100 when 
mtj.RequiredLevelId>=mtj.LevelId then (100-((mtj.RequiredLevelId-mtj.LevelId)*100/mtj.RequiredLevelId)) else 0 end)/Count(mtj.id) else 0 end as Joinability,ms.AadhaarVerification,(IsNull(ul.FirstName,'') +SPACE(1)+ ISNULL(ul.LastName,'')) as Name,
ad.Name as AadhaarVerificationDone,sl.PartitionKey as SMCQId     
from MCQShareList sl left join MCQCandidates ca on sl.EmailAddress=ca.CandidateEmail and sl.MCQID=ca.MCQId       
left join MCQ_TransSelectability mts on mts.MCQID=sl.MCQID and mts.CandidateEmail=sl.EmailAddress      
left join MCQ_TransJoinability mtj on mtj.MCQID=sl.MCQID and mtj.CandidateEmail=sl.EmailAddress      
join MCQSets ms on ms.PartitionKey=sl.MCQId    
left join UserList ul on ul.EmailId=sl.EmailAddress  
left join AadhaarDetails ad on ad.email=sl.EmailAddress
where sl.MCQId=@MCQPId and ISNULL(sl.ShareType,0) =0       
group by sl.EmailAddress,sl.ClientHR,sl.ApplyingCompany,ca.TotalExprience,ca.Skills,ca.PartitionKey,            
ca.CandidateScore,ca.TotalQuestions,sl.PartitionKey,sl.MCQId,ca.VideoLink,ca.Feedbackreating,ca.FeedbackComment,sl.ShareDate,sl.roomId,ca.RecruiterComment,ms.AadhaarVerification,(IsNull(ul.FirstName,'') +SPACE(1)+ ISNULL(ul.LastName,'')),ad.Name  
end
END  
GO