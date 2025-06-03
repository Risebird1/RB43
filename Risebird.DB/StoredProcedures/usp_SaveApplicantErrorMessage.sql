IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveApplicantErrorMessage'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveApplicantErrorMessage
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_SaveApplicantErrorMessage]     
@id int,
@ErrorMessage varchar(200),
@timeNow datetime
As
Begin

if(@ErrorMessage is not null and @ErrorMessage != '')
Begin
	update InterviewerDirectApplication set ErrorMessage = @ErrorMessage, ModifiedAt = @timeNow, ModifiedBy  = 'AI Screening Applicant'              
	where id=@id
End

End
GO