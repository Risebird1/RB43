IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewMeetingsHistory')
BEGIN

SELECT * INTO InterviewMeetingsHistory
FROM InterviewMeetings
WHERE 1 = 0;


alter table InterviewMeetingsHistory
drop column id

alter table InterviewMeetingsHistory
add id int

End
Else
Begin


IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ActionType')
      AND Object_ID = Object_ID(N'InterviewMeetingsHistory'))
BEGIN
    ALTER TABLE InterviewMeetingsHistory
    Add ActionType varchar(15)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PerformedAt')
      AND Object_ID = Object_ID(N'InterviewMeetingsHistory'))
BEGIN
    ALTER TABLE InterviewMeetingsHistory
    Add PerformedAt datetime
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PerformedBy')
      AND Object_ID = Object_ID(N'InterviewMeetingsHistory'))
BEGIN
    ALTER TABLE InterviewMeetingsHistory
    Add PerformedBy varchar(50)
END

End

Go