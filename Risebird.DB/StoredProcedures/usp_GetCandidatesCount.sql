/****** Object:  StoredProcedure [dbo].[usp_GetCandidatesCount]    Script Date: 30-07-2020 8.07.46 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidatesCount'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidatesCount
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetCandidatesCount]  
AS  
Begin 

declare @commonTable table (EmailId varchar(100),imid int, icid int, MeetingStatus int, InterviewStatus int);
		insert into @commonTable 
		select EmailId,im.id, ic.id,ic.MeetingStatus, ic.InterviewStatus 
		from CandidatesMaster cm join UserList ul on ul.id=cm.UserId join InterviewMeetings im on im.candidateEmail=ul.EmailId 
		left join InterviewComments ic on ic.InterviewId=im.id where confirmationStatus=1 

select
(select count(distinct(EmailId)) from @commonTable where InterviewStatus=1) as Proxy,
(select count(distinct(EmailId)) from @commonTable where InterviewStatus=2) as Selected,
(select count(distinct(EmailId)) from @commonTable where InterviewStatus=3) as Rejected,
(select count(distinct(EmailId)) from @commonTable where InterviewStatus=4) as BorderLine,
(select count(distinct(EmailId)) from @commonTable where InterviewStatus is null and MeetingStatus=1) as NoShowCount,
(select count(distinct(EmailId)) from @commonTable where InterviewStatus is null and (MeetingStatus=2 or MeetingStatus is null)) as ScheduledCount,
(select count(distinct(EmailId)) from @commonTable where InterviewStatus is null and MeetingStatus=3) as DroppedCount,
(select count(distinct(EmailId)) from @commonTable where InterviewStatus is null and MeetingStatus=4) as Pending





End

GO


