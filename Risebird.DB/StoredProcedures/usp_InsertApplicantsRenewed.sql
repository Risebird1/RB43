IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InsertApplicantsRenewed'))
BEGIN
    DROP PROCEDURE dbo.usp_InsertApplicantsRenewed
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_InsertApplicantsRenewed]
@irfId int,
@isVerified bit,
@IsEmployed bit,
@EmailId [varchar](100)= null,
@Phone [varchar](120)= null,
@CompanyName [varchar](100) = null,
@FirstName [varchar](150)= null,
@LastName [varchar](150) = null,
@DOB datetime = null,
@PerformedBy varchar(200),
@ReferredBy varchar(200),
@timeNow datetime,
@result bit out,
@id int out,
@assessmentGroup varchar(100) out
As
Begin

select @result = 0

if not exists (select top 1 id from InterviewerDirectApplication where EmailId =LTRIM(RTRIM(@EmailId)) and IRFId = @irfId)
Begin
	Insert into InterviewerDirectApplication(IRFId, EmailId, Phone, CreatedAt, CreatedBy, ReferredBy, isVerified, IsEmployed, CompanyName, FirstName, LastName, DOB, EVGeneratedAt, EVGeneratedBy)  
	values(@IRFId, @EmailId, @Phone, @timeNow, @EmailId, @ReferredBy, @isVerified, @IsEmployed, @CompanyName, @FirstName, @LastName, @DOB, @timeNow, @EmailId)

	select @result = 1

	set @id = SCOPE_IDENTITY()

	select @assessmentGroup = (select AssessmentGroup from AssessmentGroup where id = (select AssessmentGroupId from IRF where id = @irfId))
	
End
Else
Begin

	update InterviewerDirectApplication set isVerified = @isVerified, IsEmployed =@IsEmployed, Phone = @Phone,
	CompanyName = @CompanyName, FirstName = @FirstName, LastName =@LastName, DOB =@DOB, EVGeneratedBy=@PerformedBy, EVGeneratedAt =@timeNow, ModifiedAt = @timeNow, ModifiedBy =@EmailId where
	EmailId =LTRIM(RTRIM(@EmailId)) and IRFId = @irfId

	select @result = 1

	set @id = (select top 1 id from InterviewerDirectApplication where EmailId =LTRIM(RTRIM(@EmailId)) and IRFId = @irfId)

	select @assessmentGroup = (select AssessmentGroup from AssessmentGroup where id = (select AssessmentGroupId from IRF where id = @irfId))

End

End
GO