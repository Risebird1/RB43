/****** Object:  StoredProcedure [dbo].[usp_GetAccounts]    Script Date: 24-03-2020 8.20.45 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateAmountToZeroForRemovedInterviewers'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateAmountToZeroForRemovedInterviewers
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




CREATE PROCEDURE [dbo].[usp_UpdateAmountToZeroForRemovedInterviewers]  
@userId int,
@IRFId int,
@type varchar(30)

AS  
Begin 

if(@type='Interviewer')
Begin
	update InterviewPoints set Points = 0 where id in 
	(select distinct ipnt.id from InterviewPoints ipnt 
	join InterviewMeetings im on im.id=ipnt.Interviewid 
	join IRF on im.IRFId = irf.IRFId
	join TransInterviewer ti on ti.IRFId = IRF.id
	where irf.id=@IRFId and ti.UserId = @userId and ipnt.[Status]='Pending' and ipnt.[Type] ='Interview')
End

Else
Begin
	update InterviewPoints set Points = 0 where id in
	(select distinct ipnt.id from InterviewPoints ipnt 
	join InterviewMeetings im on im.id=ipnt.Interviewid 
	join IRF on im.IRFId = irf.IRFId
	join TransEmergencyInterviewer tei on tei.IRFId = IRF.id
	where irf.id=@IRFId and tei.UserId = @userId and ipnt.[Status]='Pending' and ipnt.[Type] ='Interview' and im.IsEmergency=1)
	
End




End
GO


