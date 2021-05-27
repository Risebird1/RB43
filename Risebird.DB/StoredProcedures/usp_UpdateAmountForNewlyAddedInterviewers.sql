/****** Object:  StoredProcedure [dbo].[usp_GetAccounts]    Script Date: 24-03-2020 8.20.45 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateAmountForNewlyAddedInterviewers'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateAmountForNewlyAddedInterviewers
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




CREATE PROCEDURE [dbo].[usp_UpdateAmountForNewlyAddedInterviewers]  
@userId int,
@IRFId int,
@PerSuccessRate int

AS  
Begin 

	update InterviewPoints set Points = Round(@PerSuccessRate/4,0) from InterviewPoints ipnt 
	join InterviewMeetings im on im.id=ipnt.Interviewid 
	join IRF on im.IRFId = irf.IRFId
	join InterviewComments ic on ic.InterviewId=im.id
	where irf.id=@IRFId and ipnt.userid=@userId and (ipnt.[Status]='Pending'or ipnt.[Status]='Submitted') and ipnt.[Type] ='Interview'
	and ic.MeetingStatus in (1,3)

	update InterviewPoints set Points = 0 from InterviewPoints ipnt 
	join InterviewMeetings im on im.id=ipnt.Interviewid 
	join IRF on im.IRFId = irf.IRFId
	join InterviewComments ic on ic.InterviewId=im.id
	where irf.id=@IRFId and ipnt.userid=@userId and (ipnt.[Status]='Pending'or ipnt.[Status]='Submitted') and ipnt.[Type] ='Interview'
	and ic.MeetingStatus in (5)

	update InterviewPoints set Points = Round(@PerSuccessRate,0) from InterviewPoints ipnt 
	join InterviewMeetings im on im.id=ipnt.Interviewid 
	join IRF on im.IRFId = irf.IRFId
	join InterviewComments ic on ic.InterviewId=im.id
	where irf.id=@IRFId and ipnt.userid=@userId and (ipnt.[Status]='Pending'or ipnt.[Status]='Submitted') and ipnt.[Type] ='Interview' 
	and VideoDuration>=60
	and ic.MeetingStatus in (4) and ic.InterviewStatus in (2,4)

	update InterviewPoints set Points = Round(@PerSuccessRate*VideoDuration/60,0) from InterviewPoints ipnt 
	join InterviewMeetings im on im.id=ipnt.Interviewid 
	join IRF on im.IRFId = irf.IRFId
	join InterviewComments ic on ic.InterviewId=im.id
	where irf.id=@IRFId and ipnt.userid=@userId and (ipnt.[Status]='Pending'or ipnt.[Status]='Submitted') and ipnt.[Type] ='Interview' 
	and VideoDuration<60
	and ic.MeetingStatus in (4) and ic.InterviewStatus in (2,4)

	update InterviewPoints set Points = Round(@PerSuccessRate*35/60,0) from InterviewPoints ipnt 
	join InterviewMeetings im on im.id=ipnt.Interviewid 
	join IRF on im.IRFId = irf.IRFId
	join InterviewComments ic on ic.InterviewId=im.id
	where irf.id=@IRFId and ipnt.userid=@userId and (ipnt.[Status]='Pending'or ipnt.[Status]='Submitted') and ipnt.[Type] ='Interview' 
	and VideoDuration>=35
	and ic.MeetingStatus in (4) and ic.InterviewStatus in (1,3)

	update InterviewPoints set Points = Round(@PerSuccessRate*VideoDuration/60,0) from InterviewPoints ipnt 
	join InterviewMeetings im on im.id=ipnt.Interviewid 
	join IRF on im.IRFId = irf.IRFId
	join InterviewComments ic on ic.InterviewId=im.id
	where irf.id=@IRFId and ipnt.userid=@userId and (ipnt.[Status]='Pending'or ipnt.[Status]='Submitted') and ipnt.[Type] ='Interview' 
	and VideoDuration<35
	and ic.MeetingStatus in (4) and ic.InterviewStatus in (1,3)


End
GO


