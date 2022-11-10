IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_CheckClientReportShareDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_CheckClientReportShareDetails
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_CheckClientReportShareDetails
@uniqueid varchar(50),  
@Emailid varchar(150),
@Interviewid int
AS  
BEGIN  
Select Count(id) from ClientReportShareDetails Where uniqueid=@uniqueid and ClientEmailid=@Emailid and Interviewid=@Interviewid 
Update ClientReportShareDetails set OpenCount=(OpenCount+1) Where uniqueid=@uniqueid and ClientEmailid=@Emailid and Interviewid=@Interviewid 
END
GO