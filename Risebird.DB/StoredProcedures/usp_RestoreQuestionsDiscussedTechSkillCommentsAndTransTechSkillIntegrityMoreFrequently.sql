/****** Object:  StoredProcedure [dbo].[usp_RestoreQuestionsDiscussedTechSkillCommentsAndTransTechSkillIntegrityMoreFrequently]     ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_RestoreQuestionsDiscussedTechSkillCommentsAndTransTechSkillIntegrityMoreFrequently'))
BEGIN
    DROP PROCEDURE dbo.usp_RestoreQuestionsDiscussedTechSkillCommentsAndTransTechSkillIntegrityMoreFrequently
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create PROCEDURE [dbo].[usp_RestoreQuestionsDiscussedTechSkillCommentsAndTransTechSkillIntegrityMoreFrequently]

    
AS
BEGIN

--query to restore TechSkillComments with QuestionsDiscussed 

	INSERT INTO dbo.TechSkillComments (IRFId, Comment, UserId, SkillName)  
	SELECT distinct  
		irf.id,  
		--IM.IRFId,                          -- maps InterviewMeetings.IRFId ? TechSkillComments.IRFId  
		QD.Comment,                        -- maps QuestionsDiscussed.Comment ? TechSkillComments.Comment  
		UL.id AS UserId,                   -- UserList.id ? TechSkillComments.UserId  
		QD.SkillName                       -- maps QuestionsDiscussed.SkillName ? TechSkillComments.SkillName  
	FROM dbo.QuestionsDiscussed QD  
	INNER JOIN dbo.InterviewMeetings IM   
		ON QD.InterviewId = IM.id  
	 inner join irf on irf.irfid = im.irfid  
	INNER JOIN dbo.UserList UL  
		ON IM.Interviewer = UL.EmailId  
	join TransTechSkill tts on (tts.SkillName = qd.SkillName and tts.irfid = irf.id )

	WHERE NOT EXISTS (  
		SELECT 1  
		FROM dbo.TechSkillComments TSC  
	 join IRF on irf.id = tsc.IRFId  
		WHERE irf.IRFId = IM.IRFId  
		  AND ISNULL(TSC.Comment, '') = ISNULL(QD.Comment, '')  
		  AND ISNULL(TSC.SkillName, '') = ISNULL(QD.SkillName, '')  
		  AND TSC.UserId = UL.id  
	) and cast(IM.interviewDate as date) > cast(dateadd(day, -40, getdate()) as date)
      

	  --query to update skill names against questions in questions discussed table for the ones that dont match with the skill names for the same questions in techskillcomments by the same interviewer
	UPDATE qd
	SET qd.SkillName = tsc.SkillName
	FROM QuestionsDiscussed qd
	JOIN InterviewMeetings im 
		ON im.Id = qd.InterviewId
	JOIN UserList ul 
		ON ul.EmailId = im.Interviewer
	JOIN irf 
		ON irf.IRFId = im.IRFId
	JOIN TechSkillComments tsc 
		ON  tsc.Comment = qd.Comment
		AND tsc.IRFId = irf.Id
		AND tsc.UserId = ul.Id
	WHERE im.InterviewDate > DATEADD(DAY, -7, GETDATE())
	  AND qd.SkillName <> tsc.SkillName;

	  delete from TechSkillComments where id in ( SELECT tsc.id
	FROM TechSkillComments tsc
	WHERE tsc.Skill IS NULL
	  AND EXISTS (
			SELECT 1
			FROM TechSkillComments tsc2
			WHERE tsc2.UserId  = tsc.UserId
			  AND tsc2.IRFId   = tsc.IRFId
			  AND tsc2.Comment = tsc.Comment
			  AND tsc2.Skill   = 0
  ))


END

GO


