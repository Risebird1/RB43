/****** Object:  StoredProcedure [dbo].[usp_AddTransInterviewerSkillsLeadInterviewer]    Script Date: 5/15/2020 10:18:01 AM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddTransInterviewerSkillsLeadInterviewer'))
BEGIN
    DROP PROCEDURE dbo.usp_AddTransInterviewerSkillsLeadInterviewer
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_AddTransInterviewerSkillsLeadInterviewer]    
    
@InterviewerSkillId int,    
@InterviewerUserId int ,  
@InterviewerPerSuccessRate int  
    
AS    
Begin    
    
    
insert into [dbo].[TransInterviewerSkillsLeadInterviewer] values (@InterviewerSkillId,(select id from [dbo].[InterviewerProfile] where userid=@InterviewerUserId), @InterviewerUserId,@InterviewerPerSuccessRate)    
    
End 
Go



