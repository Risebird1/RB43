/****** Object:  StoredProcedure [dbo].[usp_GetCandidateIdByEmail]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateIdAndIrfIdByMeetingId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateIdAndIrfIdByMeetingId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
  
CREATE PROCEDURE [dbo].[usp_GetCandidateIdAndIrfIdByMeetingId]  
  
@interviewId int 
  
AS  
Begin  

declare  

@UserId int = (select id from UserList where EmailId=(select candidateEmail from InterviewMeetings where id = @interviewId) and idProfile=23)  


declare @candidateId int, @irfId int

set @irfId  = (select id from IRF where IRFId = (select IRFID from InterviewMeetings where id = @interviewId))
set @candidateId  = (select id from CandidatesMaster where UserId = @UserId)


select @candidateId as CandidateMasterId, @irfId as IRFId
  

End  
  
  
GO


