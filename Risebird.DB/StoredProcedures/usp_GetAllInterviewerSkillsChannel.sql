/****** Object:  StoredProcedure [dbo].[usp_GetAllInterviewerSkillsChannel]    Script Date: 25-05-2020 10.53.35 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllInterviewerSkillsChannel'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllInterviewerSkillsChannel
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetAllInterviewerSkillsChannel]  
@EmailId varchar(200),   
@Profile varchar(100),
@timeNow datetime
AS  
Begin 

DECLARE @tbl_ConfirmedMeetings TABLE (  
    ISCId INT,  
    ConfirmedMeetings int   
);  
INSERT INTO @tbl_ConfirmedMeetings  
select ISC.id as ISCId, count(im.confirmationStatus) as ConfirmedMeetings   
from InterviewerSkillsChannel ISC left join UnapprovedInterviewerMeetings im on ISC.InterviewerSkillId = im.ISCId    
where im.confirmationStatus=1 group by ISC.id 


DECLARE @tbl_ScheduledMeetings TABLE (  
    ISCId INT,  
    ScheduledMeetings int   
);  
INSERT INTO @tbl_ScheduledMeetings  
select ISC.id as ISCId, count(im.id) as ScheduledMeetings   
from InterviewerSkillsChannel ISC left join UnapprovedInterviewerMeetings im on ISC.InterviewerSkillId = im.ISCId   
where im.interviewDate>@timeNow and im.confirmationStatus=1 
group by ISC.id


DECLARE @tbl_ApprovedInterviewers TABLE (  
    ISCId INT,  
    ApprovedInterviewers int   
);  
INSERT INTO @tbl_ApprovedInterviewers  
select ISC.id as ISCId, count(im.id) as ApprovedInterviewers   
from InterviewerSkillsChannel ISC join UnapprovedInterviewerMeetings im on ISC.InterviewerSkillId = im.ISCId   
join UserList ul on ul.EmailId=im.candidateEmail
join InterviewerProfile ipr on ipr.UserId=ul.id
where ipr.IsApproved=1 and im.confirmationStatus=1
group by ISC.id


--select * from InterviewerSkillsChannel
--select * from UnapprovedInterviewerMeetings
IF (@Profile = 'HR')  
	BEGIN
	
		select isc.*, Skill.[Name] as SkillName, ul.EmailId as HREmailId, tcm.ConfirmedMeetings as TotalMeetings,
		tsm.ScheduledMeetings, tai.ApprovedInterviewers
		from InterviewerSkillsChannel isc 
		left join UserList ul on ul.id = isc.RisebirdHr 
		left join Skill on Skill.id = isc.PrimarySkill
		left join @tbl_ConfirmedMeetings tcm on tcm.ISCId = isc.id
		left join @tbl_ScheduledMeetings tsm on tsm.ISCId=isc.id
		left join @tbl_ApprovedInterviewers tai on tai.ISCId = isc.id
		where ul.EmailId = @EmailId
		
	End  

Else
	begin

		select isc.*, Skill.[Name] as SkillName, ul.EmailId as HREmailId, tcm.ConfirmedMeetings as TotalMeetings,
		tsm.ScheduledMeetings, tai.ApprovedInterviewers
		from InterviewerSkillsChannel isc 
		left join UserList ul on ul.id = isc.RisebirdHr
		left join Skill on Skill.id = isc.PrimarySkill
		left join @tbl_ConfirmedMeetings tcm on tcm.ISCId = isc.id
		left join @tbl_ScheduledMeetings tsm on tsm.ISCId=isc.id
		left join @tbl_ApprovedInterviewers tai on tai.ISCId = isc.id

	End
End

GO


