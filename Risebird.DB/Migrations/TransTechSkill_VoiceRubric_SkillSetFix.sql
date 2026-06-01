-- Voice-generated rubric rows were saved with SkillSet = 0; DM Approval only displays SkillSet = 1.
-- Run once per environment if rubric-from-voice skills are missing on the Score Rubric table.

UPDATE TransTechSkill
SET SkillSet = 1
WHERE SkillSet = 0
  AND SkillName IS NOT NULL
  AND LTRIM(RTRIM(SkillName)) <> '';

GO
