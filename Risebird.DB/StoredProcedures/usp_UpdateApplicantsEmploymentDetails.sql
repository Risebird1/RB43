IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateApplicantsEmploymentDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateApplicantsEmploymentDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_UpdateApplicantsEmploymentDetails]     
@id int,
@isVerified bit,
@IsEmployed bit,
@CompanyName [varchar](100) = null,
@FirstName [varchar](150)= null,
@LastName [varchar](150) = null,
@DOB datetime = null,
@PerformedBy varchar(200),
@timeNow datetime
As
Begin

if exists (select top 1 id from InterviewerDirectApplication where id=@id)
Begin

	update InterviewerDirectApplication set isVerified = @isVerified, IsEmployed =@IsEmployed,
	CompanyName =@CompanyName, FirstName = @FirstName, LastName =@LastName, DOB =@DOB, EVGeneratedBy=@PerformedBy, EVGeneratedAt =@timeNow where id=@id

End

End
GO