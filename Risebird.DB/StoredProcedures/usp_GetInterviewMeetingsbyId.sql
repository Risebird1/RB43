/****** Object:  StoredProcedure [dbo].[usp_GetInterviewMeetingsbyId]    Script Date: 4/18/2020 10:55:15 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewMeetingsbyId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewMeetingsbyId
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_GetInterviewMeetingsbyId]      
      @interviewId int,
	  @Email varchar(max)
AS      
Begin      
  Declare @id int
Select @id= Id from UserList where EmailId=@Email
select im.id imid,im.* ,IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,IRF.ClientName,IRF.IsBiometricEnabled,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id as CandidateId,
(Select isLike from RisetubeVideoLike where userId=@Id and InterviewId=@interviewId) as IsLike,ic.TotalView as TotalView,ic.IsShared as IsShared
from InterviewMeetings im      
inner join IRF on im.IRFId = IRF.IRFId    
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId  
left join CandidatesMaster cm on im.candidateEmail = cm.EmailId   where im.id = @interviewId	
     
End  
GO


