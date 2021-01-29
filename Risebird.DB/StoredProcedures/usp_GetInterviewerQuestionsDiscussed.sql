/****** Object:  StoredProcedure [dbo].[usp_GetInterviewerQuestionsDiscussed]    Script Date: 22-01-2021 3.27.05 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviewerQuestionsDiscussed'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerQuestionsDiscussed
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create PROCEDURE [dbo].[usp_GetInterviewerQuestionsDiscussed]  
   
@InterviewId int
AS  
Begin

select * from InterviewerQuestionsDiscussed where InterviewId =@InterviewId

End
GO


