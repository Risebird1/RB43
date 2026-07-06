-- Migration: deploy stored procedures for repository inline-SQL migration (2026-06-17)
-- Run after deploying new SP files under StoredProcedures/

-- No-op marker migration; individual SP scripts are source of truth.
-- Deploy order:
--   1. usp_GetIRFWithClientDetailsById / ByStringId / usp_GetAllIRFLegacyList
--   2. usp_GetTrans*ByIRFId* family
--   3. usp_GetInterview*Report family
--   4. usp_GetEvaluator*Report family
--   5. usp_CancelInterviewMeetingById, usp_IsUserActiveByEmail, usp_GetUserIdByEmailId (existing)

PRINT 'Repository SP migration: deploy individual usp_* scripts from StoredProcedures folder.';
GO
