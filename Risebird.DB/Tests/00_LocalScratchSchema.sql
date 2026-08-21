-- =============================================================================
-- Local scratch database for the AI question set proofs.
--
-- FOR A THROWAWAY LOCAL INSTANCE ONLY. This drops and recreates a database
-- called RisebirdQSetTest. Never point it at a real Risebird database.
--
-- Why it exists: usp_SaveAIQuestionSets_RejectionTests.sql needs a database
-- holding the real IRF, TransTechSkill, SkillChannelSampleQuestions and
-- AIQuestionSet* schema. The three tables created below are stand-ins for the
-- only foreign key parents that schema requires -- IRF points at Client and
-- UserList, TransTechSkill points at LevelTech -- so the real scripts can be
-- applied to an empty instance without dragging in the rest of the database.
--
-- Run order, from the repository root, against a local instance:
--
--   $S = '(localdb)\MSSQLLocalDB'
--   sqlcmd -S $S -E -b -i Risebird.DB\Tests\00_LocalScratchSchema.sql
--   foreach ($f in @(
--       'Risebird.DB\Tables\IRF.sql',
--       'Risebird.DB\Tables\TransTechSkill.sql',
--       'Risebird.DB\Tables\SkillChannelSampleQuestions.sql',
--       'Risebird.DB\Tables\AIQuestionSets.sql',
--       'Risebird.DB\Tables\AIQuestionSetItems.sql',
--       'Risebird.DB\Tables\InterviewAIQuestionSet.sql',
--       'Risebird.DB\TableTypes\AIQuestionSetItems_TableType.sql',
--       'Risebird.DB\Migrations\Backfill_UseAIQuestionSets_ForChannelsWithSets.sql',
--       'Risebird.DB\StoredProcedures\usp_SaveAIQuestionSets.sql',
--       'Risebird.DB\StoredProcedures\usp_AssignAIQuestionSetToInterview.sql',
--       'Risebird.DB\StoredProcedures\usp_SetAIQuestionSetsEnabled.sql')) {
--       sqlcmd -S $S -E -b -d RisebirdQSetTest -i $f
--   }
--   sqlcmd -S $S -E -b -d RisebirdQSetTest -i Risebird.DB\Tests\usp_SaveAIQuestionSets_RejectionTests.sql
--   sqlcmd -S $S -E -b -d RisebirdQSetTest -i Risebird.DB\Tests\usp_AssignAIQuestionSetToInterview_FlagTests.sql
--
-- One caveat this cannot cover: on an empty instance IRF is built by the
-- CREATE TABLE branch of Tables\IRF.sql, whereas a deployed database has
-- reached its shape through years of ALTER in the ELSE branch. The two differ.
-- The rejection test's pre-flight check is what protects it there: it names any
-- NOT NULL column with no default that its fixture does not supply, rather than
-- failing on an opaque INSERT error.
-- =============================================================================

IF DB_ID('RisebirdQSetTest') IS NOT NULL
BEGIN
    ALTER DATABASE RisebirdQSetTest SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RisebirdQSetTest;
END
GO

CREATE DATABASE RisebirdQSetTest;
GO

USE RisebirdQSetTest;
GO

-- Stand-ins. Only the identity columns matter; they exist to satisfy
-- FK_IRF_Client, FK_IRF_User and TransTechSkill's foreign key to LevelTech.
CREATE TABLE dbo.Client
(
    id   int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Client PRIMARY KEY,
    Name varchar(100) NULL
);

CREATE TABLE dbo.UserList
(
    id      int IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserList PRIMARY KEY,
    EmailId varchar(150) NULL
);

CREATE TABLE dbo.LevelTech
(
    id        int IDENTITY(1,1) NOT NULL CONSTRAINT PK_LevelTech PRIMARY KEY,
    LevelName varchar(100) NULL
);

-- Stand-in for usp_AssignAIQuestionSetToInterview, which resolves an interview's
-- channel through IRF.IRFId = InterviewMeetings.IRFId. Only the three columns that
-- procedure reads are needed. IRFId is varchar here because it is varchar in the
-- real table: it holds the channel's business code, not IRF.id, and a test that
-- quietly made it an int would stop exercising the join the procedure performs.
CREATE TABLE dbo.InterviewMeetings
(
    id        int IDENTITY(1,1) NOT NULL CONSTRAINT PK_InterviewMeetings PRIMARY KEY,
    IRFId     varchar(200) NULL,
    SessionId uniqueidentifier NULL
);
GO

-- The rejection test picks the lowest id from Client and UserList for its
-- fixture IRF, so each needs at least one row.
INSERT INTO dbo.Client   (Name)      VALUES ('Scratch Client');
INSERT INTO dbo.UserList (EmailId)   VALUES ('scratch@example.com');
INSERT INTO dbo.LevelTech(LevelName) VALUES ('Mid');
GO
