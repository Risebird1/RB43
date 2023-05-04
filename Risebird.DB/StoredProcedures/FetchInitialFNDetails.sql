IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.FetchInitialFNDetails'))
BEGIN
    DROP PROCEDURE dbo.FetchInitialFNDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure FetchInitialFNDetails
AS
BEGIN
Declare @SubmitedPoints Table(
Comment varchar(200),
id int,
Points int
)
Declare @AdvancedPoints Table(
Comment varchar(200),
id int,
Points int
)

Insert into @SubmitedPoints
Select Distinct ul.EmailId as Comment,ul.id,Sum(ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) as Points 
from InterviewPoints ip join UserList ul on ul.id=ip.userid 
where ip.Status='Submitted' group by ul.EmailId,ul.id

Insert into @AdvancedPoints
Select Distinct ul.EmailId as Comment,ul.id,Sum(ip.Points) as Points 
from InterviewPoints ip join UserList ul on ul.id=ip.userid
where ip.Status='Advance' group by ul.EmailId,ul.id

Select sp.id,(Isnull(sp.Points,0)-Isnull(ap.Points,0)) as Points,sp.Comment From @SubmitedPoints sp left join @AdvancedPoints ap on sp.id=ap.id
END
GO