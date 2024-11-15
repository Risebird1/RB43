IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveInterviewerRelatedClickDetails'))
BEGIN
    DROP PROCEDURE dbo.[usp_SaveInterviewerRelatedClickDetails]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_SaveInterviewerRelatedClickDetails]  
@currenturl varchar(500),  
@PerformedBy varchar(150),  
@Functiontype varchar(100),  
@ButtonAction varchar(50),  
@PerformedOn datetime,
@Interviewid int,
@Interviewerid int
AS  
BEGIN  
Declare @ProfileType varchar(50)  
Select @ProfileType=pl.Name From UserList ul join Profile pl on pl.id=ul.idProfile where Emailid=@PerformedBy  
  
INSERT INTO InterviewerRelatedClickDetails (currenturl,Functiontype,ButtonAction,PerformedBy,PerformedOn,ProfileType,Interviewid,Interviewerid)  
                      VALUES (@currenturl, @Functiontype, @ButtonAction,@PerformedBy, @PerformedOn, @ProfileType,@Interviewid,@Interviewerid)  
END  
GO