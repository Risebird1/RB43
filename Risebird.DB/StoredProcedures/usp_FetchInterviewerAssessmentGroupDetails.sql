IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchInterviewerAssessmentGroupDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchInterviewerAssessmentGroupDetails
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure usp_FetchInterviewerAssessmentGroupDetails  
@emailid varchar(150)  
AS  
BEGIN  
Select AG.Assessmentgroup,AG.id as Assessmentid, mc.partitionkey as MCQId,ag.AssessmentVerificationType,ag.CodeLanguage,msl.PartitionKey as SMCQId,mcc.PartitionKey as CMCQId,cid.inviteId,iav.IsVerified,ul.EmailId  
from InterviewerProfile IP  
Cross Apply string_split(AssessmentGroupId, ',')  
inner join AssessmentGroup AG on value = AG.id   
left join UserList ul on ul.id=ip.UserId  
left join MCQSets mc on Ag.MCQSetsValue=mc.id  
left join MCQShareList msl on msl.MCQId=mc.PartitionKey and msl.EmailAddress=ul.EmailId  
left join MCQCandidates mcc on mcc.MCQId=mc.PartitionKey and mcc.CandidateEmail=ul.EmailId  
left join CodeInviteDetails cid on cid.assignmentId=ag.id and cid.candidateEmail=ul.EmailId  
left join InterviewerAssessmentVerifications iav on iav.UserId=ul.id and iav.AssessmentId=ag.id
where ul.EmailId=@emailid  
END




Go