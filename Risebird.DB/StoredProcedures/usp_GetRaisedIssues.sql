IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetRaisedIssues'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRaisedIssues
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure usp_GetRaisedIssues
AS
BEGIN
If(@profileName='TechSupport')
Begin
Select rs.*,ul.MobileNumber,ul.FirstName as Name from RaisedIssues rs left join UserList ul on ul.EmailId=rs.RequestedBy where rs.Requester not in('Interviewer','Client') and rs.RequestType not in('Operation') order by RequestedOn  desc
END
Else If(@profileName='Admin')
Begin
Select rs.*,ul.MobileNumber,ul.FirstName as Name from RaisedIssues rs left join UserList ul on ul.EmailId=rs.RequestedBy order by RequestedOn  desc 
End
Else
Begin
Select rs.*,ul.MobileNumber,ul.FirstName as Name from RaisedIssues rs left join UserList ul on ul.EmailId=rs.RequestedBy where rs.Requester in('Interviewer','Client') order by RequestedOn  desc
END
END
GO
