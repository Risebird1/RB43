IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetFLNConversionData'))
BEGIN
    DROP PROCEDURE usp_GetFLNConversionData
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_GetFLNConversionData    
@startDate datetime = null,                  
@endDate datetime = null,                          
@timeNow datetime,     
@loggedInUser varchar(200)          
AS                  
BEGIN         
    
	--idprofile 29= InterviewerManagement, 30= SeniorInterviewerManagement

	select distinct ul.EmailId, fln.TotalProfilesAdded, fln.TotalAcknowledged, pr.[Name] as ProfileName  from UserList ul
	outer apply dbo.fn_CountFLNConversionData(ul.EmailId, @timeNow, @startDate, @endDate) fln
	join [Profile] pr on ul.idProfile = pr.id
	where ul.idProfile in (29, 30)
      
END 
GO