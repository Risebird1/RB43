IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'IRFHistory')
BEGIN

SELECT * INTO IRFHistory
FROM IRF
WHERE 1 = 0;


alter table IRFHistory
drop column id

alter table IRFHistory
add id int

End
Else
Begin


IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ActionType')
      AND Object_ID = Object_ID(N'IRFHistory'))
BEGIN
    ALTER TABLE IRFHistory
    Add ActionType varchar(15)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PerformedAt')
      AND Object_ID = Object_ID(N'IRFHistory'))
BEGIN
    ALTER TABLE IRFHistory
    Add PerformedAt datetime
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PerformedBy')
      AND Object_ID = Object_ID(N'IRFHistory'))
BEGIN
    ALTER TABLE IRFHistory
    Add PerformedBy varchar(50)
END

End

Go