/****** Object:  StoredProcedure [dbo].[usp_GetMeetingDetails]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetMeetingDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetMeetingDetails
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetMeetingDetails]  
@meetingId int
AS  
Begin

	select candidateEmail, interviewDate, ClientHrContact, im.IRFId as IRFID, cl.logolink from InterviewMeetings im  join IRF on IRF.IRFId=im.irfid              
 join Client cl on cl.id=irf.Clientid  where im.id =@meetingId

End
GO


