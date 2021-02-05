  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetChannelInterviewersList'))
BEGIN
    DROP PROCEDURE dbo.usp_GetChannelInterviewersList
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
    
CREATE PROCEDURE [dbo].[usp_GetChannelInterviewersList]     
  @Channelid int    
AS    
BEGIN    
    
select ipr.id as InterviewerProfileId, ipr.UserId, ipr.ResumeLink, ipr.CurrentRole, ipr.PreferredDomain, ul.FirstName, ul.EmailId, ul.MobileNumber, tilis.Channelid,tilis.id, ipr.Experience, ipr.City,tilis.status  
from TransChannelInterviewerList tilis     
left join InterviewerProfile ipr on ipr.id = tilis.InterviewerProfileId    
left join UserList ul on ipr.UserId = ul.id    
left join ChannelManagement isc on isc.id = tilis.Channelid    
where tilis.Channelid = @Channelid    
    
END    
  Go