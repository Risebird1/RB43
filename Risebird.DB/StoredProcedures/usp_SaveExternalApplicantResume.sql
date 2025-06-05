IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveExternalApplicantResume'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveExternalApplicantResume
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_SaveExternalApplicantResume]     
@id int,
@timeNow datetime,
@resumeLink varchar(max) = null
As
Begin

if exists (select top 1 id from InterviewerDirectApplication where id=@id)
	Begin

		update InterviewerDirectApplication set ModifiedAt = @timeNow,
		ResumeLink = CASE 
                        WHEN (@resumeLink IS NOT NULL and @resumeLink != '') THEN @resumeLink 
                        ELSE ResumeLink 
                     END
		where id=@id

	End

End
GO