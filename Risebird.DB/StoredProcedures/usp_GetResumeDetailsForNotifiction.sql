IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetResumeDetailsForNotifiction'))
BEGIN
    DROP PROCEDURE dbo.usp_GetResumeDetailsForNotifiction
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
 
CREATE PROCEDURE usp_GetResumeDetailsForNotifiction
@StartDate datetime,
@EndDate datetime
AS
BEGIN
Select IRF.id,IRF.IRFId,ul.EmailId,COUNT(cr.id) as TotalResumes,ul.FirstName From CandidateResume cr join IRF on IRF.id=cr.IRFId
left join UserList ul on ul.id=IRF.RisebirdHr
Where AddedOn<=@EndDate and AddedOn>=@StartDate
Group By IRF.id,IRF.IRFId,ul.EmailId,ul.FirstName
END   

GO