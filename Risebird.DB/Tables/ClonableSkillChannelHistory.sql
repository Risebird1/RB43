IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ClonableSkillChannelHistory')
BEGIN

SELECT * INTO ClonableSkillChannelHistory
FROM ClonableSkillChannel
WHERE 1 = 0;


alter table ClonableSkillChannelHistory
drop column id

alter table ClonableSkillChannelHistory
add id int

End
Else
Begin


IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ActionType')
      AND Object_ID = Object_ID(N'ClonableSkillChannelHistory'))
BEGIN
    ALTER TABLE ClonableSkillChannelHistory
    Add ActionType varchar(15)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PerformedAt')
      AND Object_ID = Object_ID(N'ClonableSkillChannelHistory'))
BEGIN
    ALTER TABLE ClonableSkillChannelHistory
    Add PerformedAt datetime
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PerformedBy')
      AND Object_ID = Object_ID(N'ClonableSkillChannelHistory'))
BEGIN
    ALTER TABLE ClonableSkillChannelHistory
    Add PerformedBy varchar(50)
END

End

Go