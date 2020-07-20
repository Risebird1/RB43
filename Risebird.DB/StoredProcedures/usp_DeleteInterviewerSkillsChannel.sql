/****** Object:  StoredProcedure [dbo].[usp_DeleteInterviewerSkillsChannel]    Script Date: 25-05-2020 11.07.18 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_DeleteInterviewerSkillsChannel'))
BEGIN
    DROP PROCEDURE dbo.usp_DeleteInterviewerSkillsChannel
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create PROCEDURE [dbo].[usp_DeleteInterviewerSkillsChannel]

@ISCId int
AS
Begin

Update InterviewerSkillsChannel set IsDeleted =1 where id = @ISCId

End
GO


