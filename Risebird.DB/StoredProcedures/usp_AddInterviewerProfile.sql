SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create PROCEDURE [dbo].[usp_AddInterviewerProfile]

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


