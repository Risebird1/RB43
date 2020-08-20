IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchVideoCandidateSumaryDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchVideoCandidateSumaryDetails
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE Procedure usp_FetchVideoCandidateSumaryDetails        
@InterviewId int        
AS        
BEGIN        
        
Select vc.Emailid as Candidate,vc.Count,vc.Videoid,'SharedVideoPage' as VideoUrl,vc.Timespend as TimeSpend,vc.CurrentVideoTime as CurrentTime,vc.Isintrested as IsIntrested from SharedVideoCountDetails vc right join SharedInterviewComments ic on vc.videoid=ic.id
 join InterviewShareDetails  ias on ias.Emailid=vc.Emailid  
 where ic.Interviewid=@InterviewId          
union          
Select ul.EmailId as Candidate,vc.Count,vc.Videoid,'VideoPage' as VideoUrl,vc.Timespend as TimeSpend,vc.CurrentVideoTime as CurrentTime,vc.Isintrested as IsIntrested from VideoCountDetails vc join userlist ul on vc.Userid=ul.id right join InterviewShareDetails ias on ias.Emailid=ul.EmailId where vc.Videoid=@InterviewId and vc.Userid!=0          
END 
GO


