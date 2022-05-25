IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchunassignedInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchunassignedInterviews
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO    
        
Create Procedure usp_FetchunassignedInterviews
AS
BEGIN
Select im.id as Interviewid,im.interviewDate,im.candidateEmail as CandidateEmail,IRF.id as Irfid from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId where im.Confirmationstatus=1 and (interviewer is null or interviewer='') and
im.Interviewdate>=dateadd(minute,360,getdate()) and im.Interviewdate<=dateadd(minute,420,getdate())           
END
GO
              
          
