IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_ShowRedoRequestLit'))
BEGIN
    DROP PROCEDURE dbo.usp_ShowRedoRequestLit
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_ShowRedoRequestLit  
AS  
BEGIN  
Select im.*,case when ulc.LastName is null then ulc.FirstName                  
  else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))                  
  end as candidateName ,  
  ulc.EmailId as ChannelOwner  
  from RedoInterviews ri join InterviewMeetings im on ri.interviewid=im.id  
inner join IRF on im.IRFId=irf.IRFId  
left join UserList ulc on im.candidateEmail= ulc.EmailId   
where ISNULL(Approved,0)=0  
END  
GO