IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetVideoIntrestDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetVideoIntrestDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE Procedure usp_GetVideoIntrestDetails      
AS        
BEGIN          
              
Select vc.Emailid as Candidate,vc.Count,vc.Videoid,'SharedVideoPage' as VideoUrl,vc.Timespend as TimeSpend,vc.CurrentVideoTime as CurrentTime,vc.Isintrested as IsIntrested,im.candidateEmail as CandidateEmail from SharedVideoCountDetails vc join SharedInterviewComments ic on vc.videoid=ic.id
  
join interviewmeetings im on ic.interviewid=im.id  where vc.IsIntrested='Intrested' or vc.IsIntrested='Interested'          
union            
Select ul.EmailId as Candidate,vc.Count,vc.Videoid,'VideoPage' as VideoUrl,vc.Timespend as TimeSpend,vc.CurrentVideoTime as CurrentTime,vc.Isintrested as IsIntrested,im.candidateEmail as CandidateEmail from VideoCountDetails vc join userlist ul on vc.Userid=ul.id join interviewmeetings im on vc.Videoid=im.id where vc.Userid!=0 and (vc.IsIntrested='Intrested'  or vc.IsIntrested='Interested')             
union  
Select vc.EmailId as Candidate,vc.Count,vc.Videoid,'VideoPage' as VideoUrl,vc.Timespend as TimeSpend,vc.CurrentVideoTime as CurrentTime,vc.Isintrested as IsIntrested,im.candidateEmail as CandidateEmail from VideoCountDetails vc join interviewmeetings im on vc.Videoid=im.id where  vc.Userid=0 and (vc.IsIntrested='Intrested'  or vc.IsIntrested='Interested' )            
END    
GO
  
