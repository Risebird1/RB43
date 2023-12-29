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
	select cr.*,ul.Emailid as AddedByEmailId from CandidateResume cr join UserList ul on cr.AddedBy=ul.id  where cr.IRFId=@IRFId order by AddedOn desc
End

GO


