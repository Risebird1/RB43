-- Makes the two screening sections visible to the AI interviewer.
--
-- ============================================================================================
-- NO PROCEDURE CHANGE IS NEEDED. This file is kept only to record why, and to carry the
-- verification queries. An earlier revision of it told you to edit
-- usp_GetAIInterviewSpecificDetails - that was wrong on two counts and has been withdrawn.
-- ============================================================================================
--
-- WHAT WAS ACTUALLY WRONG
-- The rubric JSON handed to the assistant carried LevelId, Weightage, Concepts, Natureofquestion
-- and SkillName, but not SkillSet, so Role Fitment and Motivation to Join Fitment arrived as one
-- undifferentiated list. The gap was in C#, not in SQL: AIInterviewerRepository simply did not
-- read the column.
--
-- WHERE THE RUBRIC COMES FROM
-- usp_GetAIInterviewDetailsBySessionId - NOT usp_GetAIInterviewSpecificDetails. It returns three
-- result sets, which AIInterviewerRepository.GetAIInterviewDetailsBySessionId reads in order:
--
--   Tables[0]  interview + meeting details
--   Tables[1]  the rubric    -- "select * from TransTechSkill where IRFId = @irfId"
--   Tables[2]  the question/answer set
--
-- That rubric SELECT is "select *", so SkillSet has always been in the result set. Adding the
-- read in AIInterviewerRepository was the whole fix, and it is already deployed with the code.
--
-- usp_GetAIInterviewSpecificDetails is a different, smaller procedure: one result set carrying
-- the channel's prompt ids and thread id for the adaptive turn. It has no rubric in it, it is
-- correct as it stands, and the copy in StoredProcedures/ matches production exactly.

-- --------------------------------------------------------------------------------------------
-- Verification. Read-only. Run after the application build carrying the repository change.
-- --------------------------------------------------------------------------------------------

-- 1. Confirm the rubric SELECT is still "select *" (or still lists SkillSet). If someone narrows
--    it to an explicit column list that omits SkillSet, the two sections silently merge again.
SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_GetAIInterviewDetailsBySessionId')) AS LiveDefinition;
GO

-- 2. Screening channels should carry their parameters across both sections, each totalling 100.
--
--    SkillSet NULL with a total of 200 is the unsectioned shape: that channel kept the flat
--    rubric the template copy produced and never received the sectioned one. See
--    Repair_AIScreeningChannels_MissingProvisioning.sql.
SELECT
    irf.IRFId          AS ChannelCode,
    tts.SkillSet,
    COUNT(*)           AS Parameters,
    SUM(tts.Weightage) AS TotalWeight
FROM dbo.IRF irf
INNER JOIN dbo.TransTechSkill tts ON tts.IRFId = irf.id
INNER JOIN dbo.ClonableSkillChannel csc ON csc.id = irf.CreatedFromTemplateId
WHERE csc.IsAIAudioTemplate = 1
GROUP BY irf.IRFId, tts.SkillSet
ORDER BY irf.IRFId, tts.SkillSet;
GO
