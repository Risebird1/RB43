IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveRaisedIssueDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveRaisedIssueDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure usp_SaveRaisedIssueDetails  
@RequestType varchar(10),  
@Comment varchar(max),  
@AttachmentURL varchar(1000),  
@RequestedBy varchar(200),
@RequestedOn datetime,
@Requester varchar(50),
@interviewId int,
@NewId int = NULL OUTPUT
AS  
BEGIN  
    INSERT INTO RaisedIssues(RequestType, Comment, AttachmentURL, RequestedBy, RequestedOn, Status, Requester, interviewId)
    VALUES (@RequestType, @Comment, @AttachmentURL, @RequestedBy, @RequestedOn, 'Open', @Requester, @interviewId);

    SET @NewId = SCOPE_IDENTITY();

    INSERT INTO RaisedIssuesHistory(RaisedIssueId, Action, Comment, ActionBy, ActionByRole, ActionOn)
    VALUES (@NewId, 'Created', @Comment, @RequestedBy, @Requester, @RequestedOn);
END  
GO