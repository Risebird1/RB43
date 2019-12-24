/****** Object:  StoredProcedure [dbo].[usp_DeleteResumeEvaluator]    Script Date: 24-12-2019 6.20.44 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_DeleteResumeEvaluator'))
BEGIN
    DROP PROCEDURE dbo.usp_DeleteResumeEvaluator
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_DeleteResumeEvaluator]
@userId int

AS
Begin
declare
@id int = (select id from ResumeEvaluator where UserId=@userId)

delete from ResumeEvaluator where id =@id
delete from TransResumeEvaluator where ResumeEvaluatorId = @id
delete from UserList where id =@userId

END

GO


