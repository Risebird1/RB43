/****** Object:  StoredProcedure [dbo].[usp_GetRiseTubeMeetings]    Script Date: 4/12/2020 5:28:28 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetRiseTubeMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRiseTubeMeetings
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetRiseTubeMeetings]    
     @clientid int,
	  @Emailid varchar(100)  
AS    
Begin    
if @clientid = 0
 BEGIN  
select im.id imid,im.* ,IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,IRF.IsBiometricEnabled,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id as CandidateId,ic.ThumbnailImage as ThumbnailImage,cl.CuratedLink as CuratedLink, QCF.InterviewId as QCInterview
from InterviewMeetings im    
inner join IRF on im.IRFId = IRF.IRFId  
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId
left join CandidatesMaster cm on im.candidateEmail = cm.EmailId
left join QCFeedbackReport QCF on im.id=QCF.InterviewId
inner join client cl on IRF.ClientID=cl.id
where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null
END
else
 BEGIN  
select im.id imid,im.* ,IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,IRF.IsBiometricEnabled,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id as CandidateId,ic.ThumbnailImage as ThumbnailImage,QCF.InterviewId as QCInterview
from InterviewMeetings im  
inner join [dbo].[InterviewComments] ic on im.id = ic.InterviewId
inner join CandidatesMaster cm on im.candidateEmail = cm.EmailId
inner join QCFeedbackReport QCF on im.id=QCF.InterviewId
inner join IRF on im.IRFId = IRF.IRFId    
inner join [dbo].[TransClientUser] tc on tc.IRFId = IRF.id
inner join dbo.UserList ul on ul.id = tc.ClientUserId
where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and ul.EmailId=@Emailid and QCF.QCStatus=1
END
   
End   
GO
