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