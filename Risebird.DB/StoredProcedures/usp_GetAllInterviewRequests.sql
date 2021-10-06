IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllInterviewRequests'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetAllInterviewRequests]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO          
            
            
              
CREATE PROCEDURE [dbo].[usp_GetAllInterviewRequests]                     
@EmailId varchar(200),                       
@Profile varchar(100)                  
AS                      
Begin                     
                    
IF (@Profile = 'Client')                      
 BEGIN                      
 Select ir.*,RequiredSkills=STUFF((SELECT ',' + CONVERT(NVARCHAR(MAX), SkillId) FROM TransInterviewRequestSkills     
 WHERE IRFId = ir.id FOR xml path('')), 1, 1, ''),ul.EmailId as Assignto     
 From InterviewRequests ir left join UserList ul on ir.Assignto=ul.id where RequestedBy = @EmailId                    
 End 
Else IF (@Profile = 'HR')                      
 BEGIN                      
 Select ir.*,RequiredSkills=STUFF((SELECT ',' + CONVERT(NVARCHAR(MAX), SkillId) FROM TransInterviewRequestSkills     
 WHERE IRFId = ir.id FOR xml path('')), 1, 1, ''),ul.EmailId as Assignto     
 From InterviewRequests ir left join UserList ul on ir.Assignto=ul.id where ul.EmailId = @EmailId                    
 End  
Else                    
begin                    
Select ir.*,RequiredSkills=STUFF((SELECT ',' + CONVERT(NVARCHAR(MAX), SkillId) FROM TransInterviewRequestSkills     
 WHERE IRFId = ir.id FOR xml path('')), 1, 1, ''),ul.EmailId as Assignto     
 From InterviewRequests ir left join UserList ul on ir.Assignto=ul.id     
End                    
End       
      
   
GO  
     