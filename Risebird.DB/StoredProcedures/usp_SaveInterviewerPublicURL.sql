
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveInterviewerPublicURL'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveInterviewerPublicURL
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_SaveInterviewerPublicURL]          
@emailId varchar(100),      
@uniqueid varchar(100)      
AS          
Begin          
    
If Not Exists (Select 1 from InterviewerPublicURLs where Emailid=@emailId)        
Begin    
Insert into InterviewerPublicURLs(uniqueid,Emailid) values(@uniqueid,@emailId)      
End    
Select uniqueid from InterviewerPublicURLs where Emailid=@emailId    
End 