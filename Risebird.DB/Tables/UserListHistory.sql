IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'UserListHistory')
BEGIN

SELECT * INTO UserListHistory
FROM UserList
WHERE 1 = 0;


alter table UserListHistory
drop column id

alter table UserListHistory
add id int

End
Else
Begin


IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ActionType')
      AND Object_ID = Object_ID(N'UserListHistory'))
BEGIN
    ALTER TABLE UserListHistory
    Add ActionType varchar(15)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PerformedAt')
      AND Object_ID = Object_ID(N'UserListHistory'))
BEGIN
    ALTER TABLE UserListHistory
    Add PerformedAt datetime
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PerformedBy')
      AND Object_ID = Object_ID(N'UserListHistory'))
BEGIN
    ALTER TABLE UserListHistory
    Add PerformedBy varchar(50)
END

End

Go