/****** Object:  StoredProcedure [dbo].[usp_GetIRFISCUniqueness]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIRFISCUniqueness'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFISCUniqueness
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetIRFISCUniqueness]
@value varchar(100)

AS
Begin

declare @SkillChannels table (ChannelId varchar(100));
		
	Begin

	insert into @SkillChannels
	select IRFId from IRF where IRFId = @value

	insert into @SkillChannels
	select InterviewerSkillId from InterviewerSkillsChannel where InterviewerSkillId = @value

	

	select * from @SkillChannels

	End
		


End

GO


