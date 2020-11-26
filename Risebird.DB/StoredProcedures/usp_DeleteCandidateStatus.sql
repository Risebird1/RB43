/****** Object:  StoredProcedure [dbo].[usp_DeleteCandidateStatus]    Script Date: 25-11-2020 5.07.29 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_DeleteCandidateStatus'))
BEGIN
    DROP PROCEDURE dbo.usp_DeleteCandidateStatus
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_DeleteCandidateStatus]
@id int

AS
Begin

delete from CandidateStatus where id =@id

END

GO


