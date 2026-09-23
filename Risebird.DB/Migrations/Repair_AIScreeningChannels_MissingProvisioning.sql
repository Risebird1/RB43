-- Repairs AI screening channels whose provisioning never landed on them.
--
-- WHY THESE EXIST
-- usp_CreateSkillChannelFromTemplateV1 returns its @flatRate output parameter, not the identity of
-- the row it inserted. An earlier build of the AI screening intake read that as the new channel's
-- id, so usp_SaveAIScreeningProvisioning ran against whatever IRF happened to have id = the flat
-- rate. The screening channel itself was left with:
--
--   SkillSetsevaluate  NULL  -- Edit IRF then shows ONE Tech Skills table
--   OpenAIPromptId     NULL  -- Edit IRF fills a default in the box, so it LOOKS set
--   TransTechSkill     the flat copy V1 made from the template, SkillSet NULL on every row
--
-- The visible symptom is on Edit IRF: all ten parameters land in the single Tech Skills table and
-- total 200, so the save is refused with "Weightage total should be equal to 100."
--
-- The code no longer does this (it resolves the id with GetSkillChannelDetailsByName and refuses to
-- provision at all if that lookup fails), so this script is only for channels created before that
-- fix. Run STEP 1 first and check what comes back before running anything else.

-- ---------------------------------------------------------------------------------------------
-- STEP 1 - find affected channels. Read-only.
-- A channel created from an AI Audio template whose rubric carries no sections is affected.
-- ---------------------------------------------------------------------------------------------
SELECT
    irf.id                              AS IRFIntId,
    irf.IRFId                           AS ChannelCode,
    irf.SkillSetsevaluate,
    irf.OpenAIPromptId,
    irf.IdealCandidateProfile,
    COUNT(tts.id)                       AS RubricRows,
    SUM(CASE WHEN tts.SkillSet IS NULL THEN 1 ELSE 0 END) AS RowsWithNoSection,
    SUM(ISNULL(tts.Weightage, 0))       AS TotalWeight
FROM dbo.IRF irf
INNER JOIN dbo.ClonableSkillChannel csc ON csc.id = irf.CreatedFromTemplateId
LEFT JOIN dbo.TransTechSkill tts ON tts.IRFId = irf.id
WHERE csc.IsAIAudioTemplate = 1
GROUP BY irf.id, irf.IRFId, irf.SkillSetsevaluate, irf.OpenAIPromptId, irf.IdealCandidateProfile
HAVING SUM(CASE WHEN tts.SkillSet IS NULL THEN 1 ELSE 0 END) > 0
    OR ISNULL(irf.SkillSetsevaluate, 0) <> 2
ORDER BY irf.id DESC;
GO

-- ---------------------------------------------------------------------------------------------
-- STEP 2 - check which OTHER channel was written to by mistake, so it can be reviewed.
-- The wrongly targeted row has id = the flat rate, which is a small number, and will be carrying
-- an IdealCandidateProfile it should never have had. Read-only.
-- ---------------------------------------------------------------------------------------------
SELECT
    irf.id          AS IRFIntId,
    irf.IRFId       AS ChannelCode,
    irf.SkillSetsevaluate,
    irf.OpenAIPromptId,
    irf.IdealCandidateProfile
FROM dbo.IRF irf
WHERE irf.IdealCandidateProfile IS NOT NULL
  AND NOT EXISTS (
        SELECT 1
        FROM dbo.ClonableSkillChannel csc
        WHERE csc.id = irf.CreatedFromTemplateId
          AND csc.IsAIAudioTemplate = 1)
ORDER BY irf.id;
GO

-- ---------------------------------------------------------------------------------------------
-- STEP 3 - repair ONE screening channel. Set @ChannelCode and run inside a transaction.
--
-- This restores the two sections on the channel's own rubric by copying the sectioned rubric back
-- from the AI Audio template it was created from, and sets SkillSetsevaluate to 2. It does NOT
-- guess the prompt id or the ideal candidate profile - set those on Edit IRF, which is exactly the
-- read-write path this feature leaves open for correcting a channel in place.
--
-- The template keeps its own sectioned copy (TransClonableIRFTechSkill.SkillSet), so this is a
-- restore, not a reconstruction. If the template predates that column as well, its rows will also
-- have SkillSet NULL and this will not help - in that case rebuild the channel from the intake.
-- ---------------------------------------------------------------------------------------------
/*
DECLARE @ChannelCode varchar(200) = 'HRSCR-ERO-FA59E';   -- <<< set this
DECLARE @IRFIntId int, @TemplateId int;

SELECT @IRFIntId = irf.id, @TemplateId = irf.CreatedFromTemplateId
FROM dbo.IRF irf
WHERE irf.IRFId = @ChannelCode;

IF @IRFIntId IS NULL
BEGIN
    RAISERROR('No channel found with that code.', 16, 1);
END
ELSE IF NOT EXISTS (SELECT 1 FROM dbo.TransClonableIRFTechSkill
                    WHERE ClonableIRFId = @TemplateId AND SkillSet IN (1, 2))
BEGIN
    RAISERROR('The template for this channel has no sectioned rubric to restore from. Rebuild the channel from AIScreening/New instead.', 16, 1);
END
ELSE
BEGIN
    BEGIN TRAN;

    DELETE FROM dbo.TransTechSkill WHERE IRFId = @IRFIntId;

    INSERT INTO dbo.TransTechSkill
        ([IRFId], [LevelId], [SkillId], [Weightage], [Concepts],
         [Natureofquestion], [Timelimit], [isEliminationskill], [SkillName], [SkillSet])
    SELECT @IRFIntId, t.LevelId, t.SkillId, t.Weightage, t.Concepts,
           t.Natureofquestion, t.Timelimit, t.isEliminationskill, t.SkillName, t.SkillSet
    FROM dbo.TransClonableIRFTechSkill t
    WHERE t.ClonableIRFId = @TemplateId;

    UPDATE dbo.IRF SET SkillSetsevaluate = 2 WHERE id = @IRFIntId;

    -- Confirm both sections total 100 before committing.
    SELECT SkillSet, COUNT(*) AS Parameters, SUM(Weightage) AS TotalWeight
    FROM dbo.TransTechSkill WHERE IRFId = @IRFIntId
    GROUP BY SkillSet;

    -- COMMIT TRAN;   -- uncomment once the numbers above read 100 for SkillSet 1 and 2
    ROLLBACK TRAN;
END
*/
