    IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddOrUpdateInterviewerRecommendation'))
BEGIN
    DROP PROCEDURE dbo.usp_AddOrUpdateInterviewerRecommendation
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
      
            
CREATE PROCEDURE [dbo].[usp_AddOrUpdateInterviewerRecommendation](                  
  @interviewId int,                   
  @CompanySize varchar(50) = null,                  
  @Country varchar(200) = null,                   
  @CompanyType varchar(20) = null,                     
  @CompanyStage int=null,                  
  @SkillCategory  nvarchar(max)=null,                
  @Yearsofexperience  int=null,                
  @Senioritylevel  int=null,                
  @SkillNature  int=null,                
  @Domain varchar(100)=null,                
  @PerHourRateIndia varchar(50)=null,            
  @PerHourRateOutside varchar(50)=null,          
  @ProfileType int,          
  @CompentencyLevel int ,    
  @Userid int,
  @DomainName varchar(50)
                
  )                
AS                    
Begin Try                    
  BEGIN TRAN UpdateUserDetails                     
  if(@Userid=0)  
  Begin  
  Select @Userid=ul.id from UnapprovedInterviewerMeetings im join Userlist ul on im.candidateEmail=ul.EmailId where im.id = @InterviewId      
    End  
                  
  IF EXISTS(SELECT 1 FROM InterviewerRecommendation where Userid=@Userid)                    
   BEGIN                    
     UPDATE [InterviewerRecommendation] SET CompanySize = ISNULL(@CompanySize,CompanySize) ,                    
          Country = ISNULL(@Country,Country) ,                    
          CompanyType = ISNULL(@CompanyType, CompanyType) ,                  
                
    CompanyStage = ISNULL(@CompanyStage, CompanyStage),                
 SkillCategory=ISNULL(@SkillCategory, SkillCategory),                
 Yearsofexperience=ISNULL(@Yearsofexperience,Yearsofexperience),                
 Senioritylevel=ISNULL(@Senioritylevel,Senioritylevel),                
 SkillNature=ISNULL(@SkillNature,SkillNature),                
 Domain=ISNULL(@Domain,Domain),                
 PerHourRateIndia=ISNULL(@PerHourRateIndia,PerHourRateIndia),            
 PerHourRateOutside=ISNULL(@PerHourRateOutside,PerHourRateOutside),          
 ProfileType=@ProfileType,          
 CompentencyLevel=@CompentencyLevel,
 DomainName=@DomainName
     
 WHERE       Userid=@Userid                     
                    
                                        
     END                    
  ELSE                    
   BEGIN                    
    INSERT INTO [InterviewerRecommendation]                    
      (interviewId,                    
  CompanySize,              
        Country,              
  CompanyType,              
  CompanyStage,              
  SkillCategory,              
  Yearsofexperience,              
  Senioritylevel,              
  SkillNature,              
  Domain,              
  PerHourRateIndia,            
  PerHourRateOutside,          
  ProfileType,          
  CompentencyLevel,        
  Userid,
  DomainName)                     
    VALUES                    
      (@interviewId,                    
       @CompanySize,                    
       @Country,                  
       @CompanyType,                  
       @CompanyStage,                
    @SkillCategory,              
  @Yearsofexperience,                
  @Senioritylevel,                
  @SkillNature,                
  @Domain,                
  @PerHourRateIndia,            
  @PerHourRateOutside,          
  @ProfileType,          
  @CompentencyLevel,        
  @Userid,
  @DomainName)              
                  
              
                          
   END                 
                  
                    
                  
                       
 Commit TRAN UpdateUserDetails;                    
End Try                     
Begin Catch                    
 RAISERROR ('Error While updating Interview Comments', -- Message text.                    
   16, -- Severity.                    
   1 -- State.                    
   );                    
                    
 Rollback;                    
End Catch 
GO