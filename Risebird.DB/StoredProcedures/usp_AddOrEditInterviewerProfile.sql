/****** Object:  StoredProcedure [dbo].[usp_AddOrEditInterviewerProfile]    Script Date: 4/17/2020 10:09:01 PM ******/
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
@Location varchar(200),
@MobileNumber nvarchar(200),
@AltMobileNumber nvarchar(200),
@CurrentRole varchar(150),
@Experience decimal(10,2),
@PreferredDomain varchar(200),
@ResumeLink varchar(max),
@CodedResumeLink varchar(max),
@IsLeadInterviewer bit,
@IsActive bit,
@IsApproved bit,
@InterviewerProfileId int output

AS

If Exists (select 1 from [dbo].[InterviewerProfile] where userId = @UserId)
-- Update 
Begin

Declare @ProfileId int;

  Update [dbo].[InterviewerProfile]
  Set
			[FirstName] = @FirstName,
			[LastName] = @LastName,
			[NickName] = @NickName, 
			[EmailId] = @EmailId,
			[AltEmailID] = @AltEmailId,
			[Location] = @Location,
			[MobileNumber]=@MobileNumber,
			[AltMobileNumber] = @AltMobileNumber,
			[CurrentRole] = @CurrentRole,
			[Experience] = @Experience,
			[PreferredDomain] = @PreferredDomain,
			[ResumeLink] = @ResumeLink,
			[CodedResumeLink] = @CodedResumeLink,
			[IsLeadInterviewer] = @IsLeadInterviewer,
			[IsActive] = @IsActive,
			[IsApproved] = @IsApproved
       
	   where userId = @UserId
	
  select @ProfileId = id from [dbo].[InterviewerProfile] where UserId = @UserId;

delete from TransInterviewerSkill where InterviewerProfileId=@ProfileId

delete from TransInterviewerEducation where InterviewerProfileId=@ProfileId

delete from TransInterviewerProfessional where InterviewerProfileId=@ProfileId

delete from TransInterviewerProjects where InterviewerProfileId=@ProfileId

delete from TransInterviewerAvailability where InterviewerProfileId=@ProfileId

delete from TransInterviewerAccDetails where InterviewerProfileId=@ProfileId

delete from TransInterviewerCertificate where InterviewerProfileId=@ProfileId

End

Else
--Insert new record
Begin
	INSERT INTO [dbo].[InterviewerProfile]
           ([UserId],
			[FirstName],
			[LastName],
			[NickName], 
			[EmailId],
			[AltEmailID],
			[Location],
			[MobileNumber],
			[AltMobileNumber],
			[CurrentRole],
			[Experience],
			[PreferredDomain],
			[ResumeLink],
			[CodedResumeLink],
			[IsLeadInterviewer],
			[IsActive],
			[IsApproved])
       VALUES
           (@UserId,
			@FirstName,
			@LastName,
			@NickName,
			@EmailId,
			@AltEmailId,
			@Location,
			@MobileNumber,
			@AltMobileNumber,
			@CurrentRole,
			@Experience,
			@PreferredDomain,
			@ResumeLink,
			@CodedResumeLink,
			@IsLeadInterviewer,
			@IsActive,
			@IsApproved)

End

GO


