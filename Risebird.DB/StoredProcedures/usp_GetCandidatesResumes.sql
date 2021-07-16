/****** Object:  StoredProcedure [dbo].[usp_GetCandidatesResumes]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidatesResumes'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidatesResumes
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_GetCandidatesResumes]  
@IRFId int   
AS  
Begin
	
	select * from CandidateResume where IRFId=@IRFId order by AddedOn desc

End
GO


