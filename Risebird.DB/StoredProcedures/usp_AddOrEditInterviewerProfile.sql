/****** Object:  StoredProcedure [dbo].[usp_AddOrEditInterviewerProfile]    Script Date: 5/1/2020 3:59:20 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddOrEditInterviewerProfile'))
BEGIN
    DROP PROCEDURE [dbo].[usp_AddOrEditInterviewerProfile]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_AddOrEditInterviewerProfile]    
    
@UserId int,    
@FirstName varchar(150),    
@LastName nvarchar(150),    
@NickName varchar(100),    
@EmailId varchar(150),    
@AltEmailId varchar(150),    
@DOB DateTime,    
@Gender varchar(50),    
@AnnualSalary nvarchar(200),    
@Country varchar(200),    
@City varchar (200),    
@MobileNumber nvarchar(200),    
@AltMobileNumber nvarchar(200),    
@CurrentRole varchar(150),    
@Experience decimal(10,2),    
@PreferredDomain varchar(200),    
@GitHubLink nvarchar(max),    
@ResumeLink nvarchar(max),    
@CodedResumeLink nvarchar(max),    
@StackOverflowLink nvarchar(max),    
@LinkedinLink nvarchar(max),    
@Blog nvarchar(max),    
@IsLeadInterviewer bit,    
@IsActive bit,    
@IsApproved bit,    
@Tag varchar(200),    
@AssessmentGroupId int,  
@InterviewerProfileId int output    
    
AS    
    
If Exists (select 1 from [dbo].[InterviewerProfile] where userId = @UserId)    
-- Edit in both the tables    
Begin    
    
Declare @v_ProfileId int;    
    
  Update [dbo].[UserList]    
  SET    
   [FirstName] = @FirstName,    
   [LastName] = @LastName,    
   [EmailId] = @EmailId,    
   [IsActive] = @IsActive,    
   [MobileNumber] = @MobileNumber     
    
   where Id = @UserId;    
    
  Update [dbo].[InterviewerProfile]    
  Set    
   [NickName] = @NickName,     
   [AltEmailID] = @AltEmailId,    
   [DOB] = @DOB,    
   [Gender] = @Gender,    
   [AnnualSalary] = @AnnualSalary,    
   [Country] = @Country,    
   [City] = @City,    
   [AltMobileNumber] = @AltMobileNumber,    
   [CurrentRole] = @CurrentRole,    
   [Experience] = @Experience,    
   [PreferredDomain] = @PreferredDomain,    
   [ResumeLink] = @ResumeLink,    
   [CodedResumeLink] = @CodedResumeLink,    
   [GitHubLink] = @GitHubLink,    
   [StackOverflowLink] = @StackOverflowLink,    
   [LinkedinLink] = @LinkedinLink,    
   [Blog] = @Blog,    
   [IsLeadInterviewer] = @IsLeadInterviewer,    
   [IsApproved] = @IsApproved,    
   [Tag]=@Tag,  
   [AssessmentGroupId]=@AssessmentGroupId  
           
 where userId = @UserId;    
     
  select @v_ProfileId = id from [dbo].[InterviewerProfile] where UserId = @UserId;    
    
  delete from TransInterviewerSkill where InterviewerProfileId=@v_ProfileId    
    
  delete from TransInterviewerEducation where InterviewerProfileId=@v_ProfileId    
    
  --delete from TransInterviewerProfessional where InterviewerProfileId=@v_ProfileId    
    
  delete from TransInterviewerProjects where InterviewerProfileId=@v_ProfileId    
    
  delete from TransInterviewerAvailability where InterviewerProfileId=@v_ProfileId    
    
  delete from TransInterviewerAccDetails where InterviewerProfileId=@v_ProfileId    
    
  delete from TransInterviewerCertificate where InterviewerProfileId=@v_ProfileId    
    
End    
    
Else    
--Insert new record for Add request    
Begin    
Declare @v_User_Id int;    
    
 INSERT INTO [dbo].[UserList]    
           ([idProfile],    
      [FirstName],    
   [LastName],    
   [EmailId],    
   [IsActive],    
   [MobileNumber])    
       
       VALUES    
           (3,    
      @FirstName,    
   @LastName,    
   @EmailId,    
   @IsActive,    
   @MobileNumber)    
    
 -- Collect the user ID from UserList tbl    
 Select @v_User_Id = id from [dbo].[UserList] where EmailId = @EmailId;    
    
 INSERT INTO [dbo].[InterviewerProfile]    
           ([UserId],    
   [NickName],     
   [AltEmailID],    
   [DOB],    
   [Gender],    
   [AnnualSalary],    
   [Country],    
   [City],    
   [AltMobileNumber],    
   [CurrentRole],    
   [Experience],    
   [PreferredDomain],    
   [ResumeLink],    
   [CodedResumeLink],    
   [GitHubLink],    
   [StackOverflowLink],    
   [LinkedinLink],    
   [Blog],    
   [IsLeadInterviewer],    
   [IsApproved],    
   [Tag],  
   [AssessmentGroupId])    
       VALUES    
           (@v_User_Id,    
   @NickName,    
   @AltEmailId,    
   @DOB,    
   @Gender,    
   @AnnualSalary,    
   @Country,    
   @City,    
   @AltMobileNumber,    
   @CurrentRole,    
   @Experience,    
   @PreferredDomain,    
   @ResumeLink,    
   @CodedResumeLink,    
   @GitHubLink,    
   @StackOverflowLink,    
   @LinkedinLink,    
   @Blog,    
   @IsLeadInterviewer,    
   @IsApproved,    
   @Tag,  
   @AssessmentGroupId)    
    
End    

GO


