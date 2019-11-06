/****** Object:  StoredProcedure [dbo].[usp_GetIAllInterviewMeetings]    Script Date: 05-11-2019 6.03.39 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetIAllInterviewMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIAllInterviewMeetings
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
    
Create PROCEDURE [dbo].[usp_GetIAllInterviewMeetings]    
    
AS    
Begin    
    
select im.id imid,im.* ,IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus from InterviewMeetings im    
inner join IRF on im.IRFId = IRF.IRFId  
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId    
   
End    
   
GO

