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
Select rs.*,ul.MobileNumber,ul.FirstName as Name from RaisedIssues rs left join UserList ul on ul.EmailId=rs.RequestedBy order by RequestedOn  desc
END
GO
