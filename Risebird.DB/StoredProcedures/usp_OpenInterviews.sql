IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_OpenInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_OpenInterviews
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_OpenInterviews] 
  
AS
BEGIN
select InterviewMeetings.*, irf.JDLink from InterviewMeetings  
 inner join IRF irf on InterviewMeetings.IRFId=irf.IRFId   
 inner join [dbo].[TransInterviewer] tirf on IRF.id =tirf.IRFId  
 inner join [dbo].[Interviewer] inte on tirf.InterviewerId = inte.id  
 inner join [dbo].[UserList] ul on inte.UserId = ul.id  
 where interviewer is null and confirmationStatus = 1 and IsEmergency=0 and ul.EmailId = @emailID  
END
GO 