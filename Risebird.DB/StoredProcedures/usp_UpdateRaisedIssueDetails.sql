IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateRaisedIssueDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateRaisedIssueDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_UpdateRaisedIssueDetails 
@id int,
@Comment varchar(1000),  
@RequestedBy varchar(200),
@RequestedOn datetime
AS  
BEGIN  
Update RaisedIssues set Status='Close',StatusComment=@Comment,ClosedBy=@RequestedBy,ClosedOn=@RequestedOn where id=@id

END 
GO