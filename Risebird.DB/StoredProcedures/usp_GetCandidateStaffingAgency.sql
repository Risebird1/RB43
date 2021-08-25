/****** Object:  StoredProcedure [dbo].[usp_GetCandidateStaffingAgency]    ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateStaffingAgency'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateStaffingAgency
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create PROCEDURE [dbo].[usp_GetCandidateStaffingAgency]
@candidateId int
AS
Begin

select ProfileSource from Candidate where id = @candidateId

End

GO

