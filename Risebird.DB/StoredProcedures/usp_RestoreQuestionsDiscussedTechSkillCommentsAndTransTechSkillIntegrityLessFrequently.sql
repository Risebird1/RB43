/****** Object:  StoredProcedure [dbo].[usp_RestoreQuestionsDiscussedTechSkillCommentsAndTransTechSkillIntegrityLessFrequently]   ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_RestoreQuestionsDiscussedTechSkillCommentsAndTransTechSkillIntegrityLessFrequently'))
BEGIN
    DROP PROCEDURE dbo.usp_RestoreQuestionsDiscussedTechSkillCommentsAndTransTechSkillIntegrityLessFrequently
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create PROCEDURE [dbo].[usp_RestoreQuestionsDiscussedTechSkillCommentsAndTransTechSkillIntegrityLessFrequently]

    
AS
BEGIN

	 delete from TechSkillComments where id in 
	(

		select distinct tsc.id--, tsc.SkillName, tts.SkillName, TSC.IRFId
		from TechSkillComments tsc
		left join TransTechSkill tts on (tts.SkillName = tsc.SkillName and tts.IRFId = tsc.IRFId)
		where  tts.SkillName is null 


	
	)

--	delete from QuestionsDiscussed where id in 
--(
--	select  qd.id from QuestionsDiscussed qd 
--	join InterviewMeetings im on im.id = qd.InterviewId
--	JOIN IRF ON IRF.IRFID = IM.IRFID
--	left join TransTechSkill tts on qd.SkillName = tts.SkillName and irf.id= tts.IRFId
--	where tts.SkillName is null
--	and cast(IM.interviewDate as date) > cast(dateadd(day, -14, getdate()) as date) 
--)
END

GO


