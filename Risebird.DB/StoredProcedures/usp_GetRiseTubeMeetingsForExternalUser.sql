 IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetRiseTubeMeetingsForExternalUser'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRiseTubeMeetingsForExternalUser
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO 
CREATE PROCEDURE [dbo].[usp_GetRiseTubeMeetingsForExternalUser]          
     @clientid int  ,      
  @Emailid varchar(100)      
AS          
Begin       
DECLARE @id int      
 Select @id=id from UserList where EmailId=@Emailid      
if @clientid = 0      
 BEGIN        
       
select DISTINCT im.id imid,im.* ,IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,IRF.IsBiometricEnabled,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id as CandidateId,ic.ThumbnailImage
as ThumbnailImage,cl.CuratedLink as CuratedLink, QCF.InterviewId as QCInterview,(Select top 1 status from SuitableCandidate where InterviewId= ic.InterviewId  order by InterstedOn desc)  as IsIntrested ,(Select top 1 status from RiseTubeVedioShortList where InterviewId= ic.InterviewId  order by ShortListedOn desc)  as ShortListstatus,ts.Sharedon as SharedOn     
from InterviewMeetings im          
inner join IRF on im.IRFId = IRF.IRFId        
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId      
left join CandidatesMaster cm on im.candidateEmail = cm.EmailId      
left join QCFeedbackReport QCF on im.id=QCF.InterviewId      
inner join client cl on IRF.ClientID=cl.id      
join tblsharedVideo ts on ts.VideoId=ic.InterviewId 
where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and ts.Userid=@id      
END      
 BEGIN        
select DISTINCT im.id imid,im.* ,IRF.id as irfIntId,IRF.JDLink as JDLink,IRF.JDTiltle,IRF.ClientId, IRF.ClientName,IRF.IsBiometricEnabled,ic.VideoLink,ic.InterviewerComment,ic.IsApproved, ic.InterviewStatus,ic.MeetingStatus, cm.id as CandidateId,ic.ThumbnailImage
as ThumbnailImage,QCF.InterviewId as QCInterview,(Select top 1 status from SuitableCandidate where InterviewId= ic.InterviewId  order by InterstedOn desc) as IsIntrested ,(Select top 1 status from RiseTubeVedioShortList where InterviewId= ic.InterviewId  
order by ShortListedOn desc)  as ShortListstatus ,ts.Sharedon as SharedOn   
from InterviewMeetings im          
inner join IRF on im.IRFId = IRF.IRFId        
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId      
left join CandidatesMaster cm on im.candidateEmail = cm.EmailId      
 inner join QCFeedbackReport QCF on im.id=QCF.InterviewId      
inner join Subscribers su on IRF.id=su.IRFId    
join tblsharedVideo ts on ts.VideoId=ic.InterviewId 
where (ic.InterviewStatus =2 or ic.InterviewStatus=4) and ic.VideoLink is not null and IRF.Clientid=@clientid and ts.Userid=@id      
END      
         
End          