IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchCallRecordingInterviewer'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchCallRecordingInterviewer
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_FetchCallRecordingInterviewer  
@Emailid varchar(150)  
As  
BEGIN  
Select *,Convert(datetime,CONVERT(varchar,CalledOn,120)) as CalledOn From CallRecordingInterviewer where Interviewer=@Emailid  

END  
GO

