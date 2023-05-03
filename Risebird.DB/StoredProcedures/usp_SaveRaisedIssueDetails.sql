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
@Requester varchar(50)
AS  
BEGIN  
Insert into RaisedIssues(RequestType,Comment,AttachmentURL,RequestedBy,RequestedOn,Status,Requester)   
values(@RequestType,@Comment,@AttachmentURL,@RequestedBy,@RequestedOn,'Open',@Requester)  
END  
GO