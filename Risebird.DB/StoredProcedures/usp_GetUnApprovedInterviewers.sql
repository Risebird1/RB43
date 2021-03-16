/****** Object:  StoredProcedure [dbo].[usp_GetUnApprovedInterviewers]    Script Date: 25-05-2020 10.59.45 AM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetUnApprovedInterviewers'))
BEGIN
    DROP PROCEDURE dbo.usp_GetUnApprovedInterviewers
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


  
    
    
Create PROCEDURE [dbo].[usp_GetUnApprovedInterviewers]      
@ISCId int    
AS      
BEGIN    
declare @AssessmentGroup varchar(max)  
 set @AssessmentGroup = (select ag.AssessmentGroup from InterviewerSkillsChannel isc join AssessmentGroup ag on isc.AssessmentGroupId = ag.id where isc.id=@ISCId)    
 
  
 select * from (select d.*,tilis.ISCId, ul.FirstName, ul.EmailId from  (select * from (SELECT Id as InterviewerProfileId, UserId,CurrentRole, PreferredDomain, ResumeLink, AssessmentGroupName =     
    STUFF((SELECT ', ' + AssessmentGroupName    
           FROM (select ipr.*, ag.AssessmentGroup as AssessmentGroupName from (SELECT A.id,  
     Split.a.value('.', 'VARCHAR(100)') AS AssessmentGroup  
 FROM  (SELECT id,  
         CAST ('<M>' + REPLACE(AssessmentGroupId, ',', '</M><M>') + '</M>' AS XML) AS String  
     FROM  InterviewerProfile where IsApproved=0) AS A CROSS APPLY String.nodes ('/M') AS Split(a)) ipr left join AssessmentGroup ag on ipr.AssessmentGroup =ag.id) b     
           WHERE b.Id = a.Id    
           FOR XML PATH('')), 1, 2, '')    
 FROM (select ipr.*, ag.AssessmentGroup as AssessmentGroupName from InterviewerProfile ipr left join AssessmentGroup ag on ipr.AssessmentGroupId like '%'+ag.id+'%' where ipr.IsApproved=0) a    
 GROUP BY Id, UserId,CurrentRole, PreferredDomain, ResumeLink) c     
 where CHARINDEX(@AssessmentGroup,c.AssessmentGroupName)>0 ) d    
 join UserList ul on d.UserId = ul.id    
 left join (select * from TransInterviewerListInterviewerSkills where ISCId=@ISCId) tilis     
 on tilis.InterviewerProfileId = d.InterviewerProfileId) e    
 where e.ISCId is null
    
      
End      
  

GO


