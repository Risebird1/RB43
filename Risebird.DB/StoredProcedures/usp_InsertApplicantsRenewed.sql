IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InsertApplicantsRenewed'))
BEGIN
    DROP PROCEDURE dbo.usp_InsertApplicantsRenewed
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_InsertApplicantsRenewed]
@irfId int = 0,
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
@assessmentGroupId int = null,
@result bit out,
@id int out,
@assessmentGroup varchar(100) out
As
Begin

select @result = 0

-- Determine the effective AG id: either passed directly or derived from the IRF
DECLARE @effectiveAgId INT = @assessmentGroupId
IF (@effectiveAgId IS NULL OR @effectiveAgId = 0) AND @irfId > 0
BEGIN
    SELECT @effectiveAgId = AssessmentGroupId FROM IRF WHERE id = @irfId
END

-- Use 0 for IRFId when the application is MAG-based (no specific skill channel)
DECLARE @effectiveIrfId INT = CASE WHEN @irfId > 0 THEN @irfId ELSE 0 END

if not exists (select top 1 id from InterviewerDirectApplication where EmailId = LTRIM(RTRIM(@EmailId)) and (IRFId = @effectiveIrfId OR (@effectiveIrfId = 0 AND AssessmentGroupId = @effectiveAgId)))
Begin
	Insert into InterviewerDirectApplication(IRFId, AssessmentGroupId, EmailId, Phone, CreatedAt, CreatedBy, ReferredBy, isVerified, IsEmployed, CompanyName, FirstName, LastName, DOB, EVGeneratedAt, EVGeneratedBy)  
	values(@effectiveIrfId, @effectiveAgId, @EmailId, @Phone, @timeNow, @EmailId, @ReferredBy, @isVerified, @IsEmployed, @CompanyName, @FirstName, @LastName, @DOB, @timeNow, @EmailId)

	select @result = 1

	set @id = SCOPE_IDENTITY()

	select @assessmentGroup = (select AssessmentGroup from AssessmentGroup where id = @effectiveAgId)
	
End
Else
Begin

	update InterviewerDirectApplication set isVerified = @isVerified, IsEmployed = @IsEmployed, Phone = @Phone,
	CompanyName = @CompanyName, FirstName = @FirstName, LastName = @LastName, DOB = @DOB, EVGeneratedBy = @PerformedBy, EVGeneratedAt = @timeNow, ModifiedAt = @timeNow, ModifiedBy = @EmailId,
	AssessmentGroupId = ISNULL(@effectiveAgId, AssessmentGroupId)
	where EmailId = LTRIM(RTRIM(@EmailId)) and (IRFId = @effectiveIrfId OR (@effectiveIrfId = 0 AND AssessmentGroupId = @effectiveAgId))

	select @result = 1

	set @id = (select top 1 id from InterviewerDirectApplication where EmailId = LTRIM(RTRIM(@EmailId)) and (IRFId = @effectiveIrfId OR (@effectiveIrfId = 0 AND AssessmentGroupId = @effectiveAgId)))

	select @assessmentGroup = (select AssessmentGroup from AssessmentGroup where id = @effectiveAgId)

End

End
GO
