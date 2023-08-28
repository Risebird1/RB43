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
@AssessmentGroupId varchar(100),        
@UserPassword nvarchar(60),     
@WhatsappNumber varchar(15),    
@Isinstaavailable bit,  
@InterviewerProfileId int output,  
--@IsServiceDomain bit,  
--@IsProductDomain bit,  
@IndiaServiceRate int,  
@IndiaProductRate int,  
@OutsideIndiaServiceRate int,  
@OutsideIndiaProductRate int,
@isVerified bit,
@CompanyName varchar(100),
@IsEmployed bit,
@loggedInUser varchar(50),
@AssessmentGroupDifference varchar(100),
@assessmentGroupList varchar(max) out,
@RemovedAssessmentGroups varchar(max)=null
AS            
Begin
  set @assessmentGroupList = ''
  if(@AssessmentGroupDifference is not null and @AssessmentGroupDifference != '')
  Begin
    declare @temp table(assessmentGroupId varchar(5), TotalInterviewers int)
  
	  insert into @temp

	  select a.value, b.TotalInterviewers from string_split(@AssessmentGroupDifference,',') a
	  outer apply
	  (select count(ipr.Id) as TotalInterviewers from InterviewerProfile ipr join UserList ul on ul.id = ipr.UserId cross apply string_split(ipr.AssessmentGroupId, ',') where a.value = value and ul.IsActive=1 group by value) b


	  if exists(select top 1 assessmentGroupId from @temp where TotalInterviewers >=2000)
	  Begin
		set @assessmentGroupList = (select STRING_AGG(asg.AssessmentGroup, ',') from @temp tp join AssessmentGroup asg on asg.id = tp.assessmentGroupId where TotalInterviewers >= 2000)
		return
	  End
	   
  End
if(@assessmentGroupList is null or @assessmentGroupList = '')
Begin
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
	   [MobileNumber] = @MobileNumber,             
		[WhatsappNumber]=@WhatsappNumber        
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
	   [AssessmentGroupId]=@AssessmentGroupId,  
	   [Isinstaavailable]=@Isinstaavailable,
	   [ModifiedBy] = @loggedInUser,
	   [ModifiedOn] = dateadd(MINUTE, 330, getdate())
	   --[IsServiceDomain] =@IsServiceDomain,  
	   --[IsProductDomain] = @IsProductDomain,  
	   --[IndiaServiceRate] = @IndiaServiceRate,  
	   --[IndiaProductRate] = @IndiaProductRate,  
	   --[OutsideIndiaServiceRate] = @OutsideIndiaServiceRate,  
	   --[OutsideIndiaProductRate] = @OutsideIndiaProductRate  
	 where userId = @UserId;            
      
   
  
	  select @v_ProfileId = id from [dbo].[InterviewerProfile] where UserId = @UserId;            
            
	  delete from TransInterviewerSkill where InterviewerProfileId=@v_ProfileId            
            
	  delete from TransInterviewerEducation where InterviewerProfileId=@v_ProfileId            
            
	  --delete from TransInterviewerProfessional where InterviewerProfileId=@v_ProfileId            
            
	  delete from TransInterviewerProjects where InterviewerProfileId=@v_ProfileId            
            
	  delete from TransInterviewerAvailability where InterviewerProfileId=@v_ProfileId            
            
	  delete from TransInterviewerAccDetails where InterviewerProfileId=@v_ProfileId            
            
	  delete from TransInterviewerCertificate where InterviewerProfileId=@v_ProfileId         
        
	  select @InterviewerProfileId=@v_ProfileId   
    
	  select * from InterviewerRecommendation  
  
	  if exists(select id from InterviewerRecommendation where Userid = @UserId)  
	  begin  
	 update InterviewerRecommendation set   
	 PerHourRateIndia = @IndiaServiceRate,  
	 PerHourRateOutside =@OutsideIndiaServiceRate,  
	 IndiaProductRate =@IndiaProductRate,  
	 OutsideIndiaProductRate =@OutsideIndiaProductRate  
	 where Userid =@UserId  
	  end  
	  else  
	  begin  
	 insert into InterviewerRecommendation (Userid, PerHourRateIndia, PerHourRateOutside, IndiaProductRate, OutsideIndiaProductRate)  
	 values (@UserId, @IndiaServiceRate, @OutsideIndiaServiceRate, @IndiaProductRate, @OutsideIndiaProductRate)  
	  end  
            
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
   [MobileNumber],      
   [UserPassword],    
   [WhatsappNumber],  
   [uniqueid])            
               
       VALUES            
           (3,            
      @FirstName,            
   @LastName,            
   @EmailId,            
   @IsActive,            
   @MobileNumber,      
   @UserPassword,    
   @WhatsappNumber,  
   NEWID())          
            
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
   [AssessmentGroupId],  
   [Isinstaavailable],
   [CreatedBy],
   [CreatedOn],
   [isVerified],
   [CompanyName],
   [IsEmployed])  
   --[IsServiceDomain],  
   --[IsProductDomain],  
   --[IndiaServiceRate],  
   --[IndiaProductRate],  
   --[OutsideIndiaServiceRate],  
   --[OutsideIndiaProductRate])            
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
   @AssessmentGroupId,  
   @Isinstaavailable,
   @loggedInUser,
   dateadd(MINUTE, 330, getdate()),
   @isVerified,
   @CompanyName,
   @IsEmployed)  
   --@IsServiceDomain,  
   --@IsProductDomain,  
   --@IndiaServiceRate,  
   --@IndiaProductRate,  
   --@OutsideIndiaServiceRate,  
   --@OutsideIndiaProductRate            
      
	select @InterviewerProfileId=(select id from InterviewerProfile where UserId=@v_User_Id)  
   
	if exists(select id from InterviewerRecommendation where Userid = @v_User_Id)  
	begin  
	 update InterviewerRecommendation set   
	 PerHourRateIndia = @IndiaServiceRate,  
	 PerHourRateOutside =@OutsideIndiaServiceRate,  
	 IndiaProductRate =@IndiaProductRate,  
	 OutsideIndiaProductRate =@OutsideIndiaProductRate  
	 where Userid =@UserId  
	end  
	else  
	begin  
	insert into InterviewerRecommendation (Userid, PerHourRateIndia, PerHourRateOutside, IndiaProductRate, OutsideIndiaProductRate)  
	values (@v_User_Id, @IndiaServiceRate, @OutsideIndiaServiceRate, @IndiaProductRate, @OutsideIndiaProductRate)  
	end  
End  
End

if(@AssessmentGroupDifference is not null and @AssessmentGroupDifference != '')
  Begin
    --insert into @temp
	  insert into AssessmentGroupInterviewers
	  select a.value, @InterviewerProfileId, 0, @loggedInUser, GETDATE(), null, null from string_split(@AssessmentGroupDifference,',') a
  End
   
if(@RemovedAssessmentGroups is not null and @RemovedAssessmentGroups != '')
  Begin
    declare @tempRemovedFromAssessmentGroups table(assessmentGroupId varchar(5))
  	  insert into @tempRemovedFromAssessmentGroups
	  	  select value from string_split(@RemovedAssessmentGroups,',')
	  
	  --delete from TransInterviewer where id in 
	  --(select * from @tempRemovedFromAssessmentGroups tra join irf on tra.assessmentGroupId = irf.AssessmentGroupId
	  --join TransInterviewer ti on ti.IRFId = irf.id where ti.UserId = @UserId)
	  
	  update TransInterviewer set IsActive = 0 where id in 
	  (select ti.id from @tempRemovedFromAssessmentGroups tra join irf on tra.assessmentGroupId = irf.AssessmentGroupId
	  join TransInterviewer ti on ti.IRFId = irf.id where ti.UserId = @UserId)
   End 
End
GO

