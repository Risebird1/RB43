/****** Object:  StoredProcedure [dbo].[usp_DeleteCandidateResume]   ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_DeleteCandidateResume'))
BEGIN
    DROP PROCEDURE dbo.usp_DeleteCandidateResume
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_DeleteCandidateResume]
@id int

AS
Begin

delete from CandidateResume where id=@id

END

GO


