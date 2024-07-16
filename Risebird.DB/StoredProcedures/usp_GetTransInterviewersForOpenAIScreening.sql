IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetTransInterviewersForOpenAIScreening'))
BEGIN
    DROP PROCEDURE dbo.usp_GetTransInterviewersForOpenAIScreening
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetTransInterviewersForOpenAIScreening]     
As
Begin

 select distinct top 3 ti.id, ResumeLink, ul.EmailId, irf.id as IRFId, 
 irf.JDTiltle as JDTitle, irf.CreatedOn from TransInterviewer ti
 join IRF on irf.id = ti.irfId
 join InterviewerProfile ipr on ipr.id = ti.InterviewerId
 join UserList ul on ul.id = ipr.UserId
 where ti.IsActive = 1 and OpenAIScreeningResponse is null--irfid = @irfId and ti.UserId = @userId
 order by irf.CreatedOn desc

End
GO