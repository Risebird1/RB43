-- Group D item 12: Job Level → experience bands (enum descriptions updated in code).

-- Stored value: IRF.JobLevel INT (1–6). Display labels come from JobLevels enum / JobLevelHelper.

--

-- Canonical mapping (numeric id unchanged where possible):

--   1 = 1–3 years   (legacy: SE, SE1)

--   2 = 3–6 years   (legacy: SSE, SE2)

--   3 = 6–9 years   (legacy: Lead, SE3)

--   4 = 9–12 years  (legacy: Manager, SE4)

--   5 = 12–15 years (legacy: Architect, SE5)

--   6 = 15+ years   (legacy: Staff, Principal)

--

-- Existing rows with JobLevel 1–5 need no update (same ids, new labels).

-- Run only after product confirms any legacy ids above 6:



-- UPDATE IRF SET JobLevel = 6 WHERE JobLevel IN (7, 8); -- example Staff/Principal if stored separately



GO

