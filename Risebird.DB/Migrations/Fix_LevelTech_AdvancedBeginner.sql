-- LevelTech id 3 is Dreyfus "Advanced Beginner" - the second-lowest rung - but was stored and shown as
-- "Advanced", which reads as better than Competent and inverts two rungs on the client report.
--
-- Ships in the same release as the LevelTechEnum / LevelTechForReport Display(Name) and Description
-- change, so the report (which reads LevelTech.Name via usp_GetInterviewTechSkillReport) and the channel
-- config / feedback dropdowns (which read the enum) agree. Renaming only one of the two is how they
-- drifted apart in the first place.
--
-- Display-name fix only: LevelTech.id is not touched. Idempotent, and only renames the row if it still
-- holds the old name, so an environment where someone already corrected it is left alone.

-- What the row holds before the fix - check this before running in each environment.
SELECT id, [Name] FROM dbo.LevelTech WHERE id = 3;

UPDATE dbo.LevelTech
SET [Name] = 'Advanced Beginner'
WHERE id = 3
  AND LTRIM(RTRIM([Name])) = 'Advanced';

SELECT id, [Name] FROM dbo.LevelTech WHERE id = 3;
GO
