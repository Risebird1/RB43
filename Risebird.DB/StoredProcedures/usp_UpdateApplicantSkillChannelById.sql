IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateApplicantSkillChannelById'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateApplicantSkillChannelById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UpdateApplicantSkillChannelById]    
@id int,
@IRFId int,
@ModifiedBy varchar(100),
@timeNow datetime
 
AS    
Begin  

begin try
	begin tran

			Update InterviewerDirectApplication set IRFId = @IRFId,  ModifiedAt = @timeNow, ModifiedBy = @ModifiedBy
			where id = @id

	commit tran
end try
begin catch
	rollback transaction    
	DECLARE @Message varchar(MAX) = ERROR_MESSAGE(),    
	@Severity int = ERROR_SEVERITY(),    
	@State smallint = ERROR_STATE()    
	RAISERROR (@Message, @Severity, @State)
end catch



End
GO

