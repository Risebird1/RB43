-- Makes the two screening sections visible to the AI interviewer.
--
-- WHAT IS MISSING
-- Provisioning splits the rubric across SkillSet 1 (Role Fitment) and 2 (Motivation to Join
-- Fitment) correctly, but the rubric JSON handed to the assistant carries only LevelId, Weightage,
-- Concepts, Natureofquestion and SkillName. Without SkillSet the assistant cannot tell the two
-- sections apart and treats all ten parameters as one undifferentiated list.
--
-- The C# half is already done: AIInterviewerRepository adds obj.SkillSet to the rubric JSON when
-- the column is present in the result set. It is guarded on the column existing, so it is a no-op
-- until this change is applied, and it affects no other channel type afterwards.
--
-- ============================================================================================
-- READ THIS BEFORE EDITING usp_GetAIInterviewSpecificDetails
-- ============================================================================================
-- The copy of that procedure in StoredProcedures/usp_GetAIInterviewSpecificDetails.sql is STALE.
-- It returns ONE result set, while AIInterviewerRepository reads three:
--
--   Tables[0]  channel + prompt ids   (the only one the repo's copy returns)
--   Tables[1]  the rubric             <-- SkillSet belongs here
--   Tables[2]  the question/answer set
--
-- Deploying the repo's copy as-is would drop the rubric and the Q&A result sets and break every
-- AI interview. Do NOT run that file. Script the procedure out of the target database, add the
-- column to the rubric SELECT in that live definition, and deploy that.
--
-- The change itself is one column on the rubric SELECT:
--
--     SELECT  tts.LevelId
--           , tts.Weightage
--           , tts.Concepts
--           , tts.Natureofquestion
--           , tts.SkillName
--           , tts.SkillSet          -- <<< ADD THIS
--     FROM dbo.TransTechSkill tts
--     WHERE tts.IRFId = @irfIntId
--     ...
--
-- TransTechSkill.SkillSet already exists, and is NULL on every channel that is not an AI
-- screening channel, so adding it to the result set changes nothing for them.

-- --------------------------------------------------------------------------------------------
-- Verification. Run after deploying the procedure change.
-- --------------------------------------------------------------------------------------------

-- 1. The rubric result set should now carry SkillSet. Replace the session id with a real one from
--    an AI screening interview.
-- EXEC dbo.usp_GetAIInterviewSpecificDetails @SessionId = '00000000-0000-0000-0000-000000000000';

-- 2. Screening channels should have their parameters split across both sections, each totalling 100.
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
