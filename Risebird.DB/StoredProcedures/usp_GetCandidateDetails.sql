/****** Object:  StoredProcedure [dbo].[usp_GetCandidateDetails]    Script Date: 16-07-2020 5.01.53 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetCandidateDetails]

@imid int

AS
Begin

select ul.[FirstName] as [Name], ca.Skill from InterviewMeetings im join irf on im.IRFId=irf.IRFId
join UserList ul on im.candidateEmail = ul.EmailId
join CandidatesMaster cm on cm.UserId = ul.id
join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id
where im.id =@imid

End
GO


