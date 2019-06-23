
:SETVAR path "C:\Sample Sol\Risebird\Risebird.DB"
SET NOCOUNT ON;

PRINT 'Start: OneShare Database update...';

:r $(path)\Tables\_CreateOrAlterTables_DB.sql
:r $(path)\StoredProcedures\_CreateOrAlterSP.sql
:r $(path)\Views\_CreateorAlterViews.sql
:r $(path)\Functions\_CreateOrAlterFunctions.sql

PRINT 'End: Database update...';
