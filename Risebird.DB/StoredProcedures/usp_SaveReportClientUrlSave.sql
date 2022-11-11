IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveReportClientUrlSave'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveReportClientUrlSave
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

    
    
Create PROCEDURE [dbo].[usp_SaveReportClientUrlSave]        
        
@emailId varchar(150),    
@uniqueid varchar(50),    
@Interviewid int        
AS        
Begin        
  
If Not Exists (Select 1 from ClientReportShareDetails where ClientEmailid=@emailId and Interviewid=@Interviewid)      
Begin  
Insert into ClientReportShareDetails(uniqueid,ClientEmailid,Interviewid,SharedOn) values(@uniqueid,@emailId,@Interviewid,GetDate())    
End  
Select uniqueid from ClientReportShareDetails where ClientEmailid=@emailId and Interviewid=@Interviewid  
End
GO