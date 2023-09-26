IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetReliabilityScoreDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetReliabilityScoreDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetReliabilityScoreDetails
@Emailid varchar(200)
AS
BEGIN
Select ri.Name as ReliabilityScoreType,Sum(ti.Point) as Point,Count(ti.id) as TotalCount 
from TransInterviewersReliabilityScores ti join ReliabilityScoreTypes ri on ri.id=ti.ReliabilityScoreTypeId
join InterviewerProfile ip on ip.Id=ti.InterviewerId
join UserList ul on ul.id=ip.Userid
where ul.EmailId=@Emailid
group by ri.Name 
END
GO