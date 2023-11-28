IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchAudioComment'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchAudioComment
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_FetchAudioComment
@interviewId int
as 
begin
Select irf.AudioComment,irf.CompanyDetails,im.zoomLink,im.interviewDate,irf.JDLink,irf.JobLevel from InterviewMeetings im                   
 join IRF  irf on im.IRFId=irf.IRFId where im.id=@interviewId
 End
GO