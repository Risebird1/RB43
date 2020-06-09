IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_DeleteInterview'))
BEGIN
    DROP PROCEDURE dbo.usp_DeleteInterview
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_DeleteInterview]       
  @imid int     
        
AS        
Begin        
        
delete from dbo.InterviewReport where InterviewId= @imid
delete from dbo.InterviewComments where InterviewId= @imid
delete from ProctoringReport where InterviewId= @imid
delete from TechSkillReport where InterviewId= @imid
delete from SkillGeneralReport where Interviewid= @imid
delete from QuestionsDiscussed where InterviewId= @imid
delete from InterviewMeetings where id= @imid   
END
GO 