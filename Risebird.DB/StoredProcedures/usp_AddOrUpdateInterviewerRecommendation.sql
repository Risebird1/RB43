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
  @CompanyStage varchar(25)=null,                      
  @SkillCategory  nvarchar(max)=null,                    
  @Yearsofexperience  varchar(25)=null,                    
  @Senioritylevel  varchar(25)=null,                    
  @SkillNature  int=null,                    
  @Domain varchar(100)=null,                    
  @PerHourRateIndia varchar(50)=null,                
  @PerHourRateOutside varchar(50)=null,              
  @ProfileType int,              
  @CompentencyLevel int ,        
  @Userid int,   
  @AssessmentGroupId varchar(100),  
  @CodedResumeLink varchar(max),  
 @Trained int,  
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
 Trained=ISNULL(@Trained,Trained), 
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
  DomainName,Trained)                         
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
  @DomainName, @Trained)                  
                      
                  
                              
   END                     
  declare @indiaProductRate int, @outsideIndiaProductRate int
  set @indiaProductRate = CAST((@PerHourRateIndia * 1.5) as int)
  set @outsideIndiaProductRate = CAST((@PerHourRateOutside * 1.5) as int)
  
  Update InterviewerProfile set AssessmentGroupId=@AssessmentGroupId,CodedResumeLink=@CodedResumeLink, IndiaProductRate = @indiaProductRate,
  OutsideIndiaProductRate = @outsideIndiaProductRate where UserId=@Userid                      
                      
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