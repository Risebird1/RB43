-- Bounce & Dropped Mail – Candidate Actions: database deployment (run with sqlcmd from Risebird.DB\, SQLCMD mode).
--
--   sqlcmd -S <server> -d <database> -U <user> -P <password> -I -b -i Scripts\Deploy_BounceDroppedCandidateActions.sql
--
-- Deploy the database BEFORE the web app. Every change is additive: the reworked list SP still answers the
-- old app's call (no @PageSize) with every row and MailBody, so the old app keeps working in between.
-- Run step 2 in a low-traffic window (index build on SendgridEventData).
-- Rollback: redeploy the previous app and re-run the previous usp_GetEmailRecipientsActivityBounceDropped.sql
-- from git; the new tables, functions and procedures can stay.

-- 1. New tables (T1)
:r Tables\MailBounceResolution.sql
:r Tables\CandidateEmailChangeLog.sql

-- 2. Widen SendgridEventData.email to varchar(150), add IX_SendgridEventData_Event_CreatedAt (T5, bugs #7, #8)
:r Migrations\BounceDropped_SendgridEventData_EmailWiden_Index.sql
:r StoredProcedures\usp_SaveSendGridEventDetails.sql

-- 3. Shared scope rule + core row set
:r Functions\fn_BounceDroppedScopeAllows.sql
:r Functions\fn_GetBounceDroppedRows.sql

-- 4. New procedures (T2, T3, T22, access check, mail content)
:r StoredProcedures\usp_GetBounceDroppedRowForAction.sql
:r StoredProcedures\usp_UpdateBouncedCandidateEmail.sql
:r StoredProcedures\usp_ResolveMailBounce.sql
:r StoredProcedures\usp_GetBounceMailContent.sql
:r StoredProcedures\usp_GetBounceDroppedFilterOptions.sql

-- 5. Reworked list procedure (T4; includes the InterviewerId column for Edit Interviewer)
:r StoredProcedures\usp_GetEmailRecipientsActivityBounceDropped.sql
