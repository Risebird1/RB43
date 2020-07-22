/****** Object:  StoredProcedure [dbo].[usp_DeleteInterviewerEmployer]    Script Date: 5/1/2020 4:05:25 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_DeleteInterviewerEmployer'))
BEGIN
    DROP PROCEDURE [dbo].[usp_DeleteInterviewerEmployer]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO







CREATE PROCEDURE [dbo].[usp_DeleteInterviewerEmployer] @Id int
AS
Begin Try
  BEGIN TRAN deleteEmployer	
		
	IF EXISTS (SELECT 1 FROM TransInterviewerProfessional where Id = @Id) 
		BEGIN
		    delete FROM TransInterviewerProfessional where Id = @Id
		END
  Commit TRAN deleteEmployer;
  End Try 

  Begin Catch
	RAISERROR ('Error While Deleting Employer Details', -- Message text.
			16, -- Severity.
			1 -- State.
			);

	Rollback;

  End Catch



GO

