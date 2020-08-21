IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchVideoCandidateSumaryDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchVideoCandidateSumaryDetails
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE Procedure usp_FetchVideoCandidateSumaryDetails        
@InterviewId int,
@Userid varchar(200)
AS        
BEGIN        
Select ias.EmailId as Candidate,vc.Count,vc.Videoid,'VideoPage' as VideoUrl,vc.Timespend as TimeSpend,vc.CurrentVideoTime as CurrentTime,vc.Isintrested as IsIntrested 
from InterviewShareDetails ias inner join userlist ul on ul.id=ias.SharedBy
left join VideoCountDetails vc on vc.Videoid=ias.Interviewid and vc.Emailid=ias.Emailid
Where ul.emailid=@Userid and ias.Interviewid=@InterviewId
END 
GO


