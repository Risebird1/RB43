/****** Object:  StoredProcedure [dbo].[usp_AddRecruiter]    Script Date: 08-01-2020 6.44.13 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetRiseTubeMeetingsForCuratedUser'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRiseTubeMeetingsForCuratedUser
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetRiseTubeMeetingsForCuratedUser]        
     @clientid int  ,    
  @Emailid varchar(100)    
AS        
Begin     
DECLARE @id int    
 Select @id=id from UserList where EmailId=@Emailid    
if @clientid = 0    
 BEGIN      
     
select im.id imid,im.* ,IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,IRF.IsBiometricEnabled,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id as CandidateId,ic.ThumbnailImage 
  
as ThumbnailImage,cl.CuratedLink as CuratedLink, QCF.InterviewId as QCInterview,sc.Status as IsIntrested ,rt.Status as ShortListstatus  
from InterviewMeetings im        
inner join IRF on im.IRFId = IRF.IRFId      
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId    
left join CandidatesMaster cm on im.candidateEmail = cm.EmailId    
left join QCFeedbackReport QCF on im.id=QCF.InterviewId    
inner join client cl on IRF.ClientID=cl.id    
inner join Subscribers su on IRF.id=su.IRFId   
left join SuitableCandidate sc on sc.InterviewId=ic.InterviewId  
left join RiseTubeVedioShortList rt on rt.InterviewId=ic.InterviewId  
where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and su.Userid=@id    
END    
else    
 BEGIN      
select im.id imid,im.* ,IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,IRF.IsBiometricEnabled,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id as CandidateId,ic.ThumbnailImage 
  
as ThumbnailImage,QCF.InterviewId as QCInterview,sc.Status as IsIntrested ,rt.Status as ShortListstatus  
from InterviewMeetings im        
inner join IRF on im.IRFId = IRF.IRFId      
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId    
left join CandidatesMaster cm on im.candidateEmail = cm.EmailId    
 inner join QCFeedbackReport QCF on im.id=QCF.InterviewId    
inner join Subscribers su on IRF.id=su.IRFId    
left join SuitableCandidate sc on sc.InterviewId=ic.InterviewId  
left join RiseTubeVedioShortList rt on rt.InterviewId=ic.InterviewId  
where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and IRF.Clientid=@clientid and su.Userid=@id    
END    
       
End        
  