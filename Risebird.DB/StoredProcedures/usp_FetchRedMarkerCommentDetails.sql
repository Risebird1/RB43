IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchRedMarkerCommentDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchRedMarkerCommentDetails
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_FetchRedMarkerCommentDetails  
@interviewid int,  
@IRFid int,
@type varchar(30)
AS  
BEGIN  
Select * From RedMarkerStatusDetails where InterviewerUserid=@interviewid and IRFId=@IRFid and type=@type 
END
Go