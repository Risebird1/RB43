IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetAllIRFAssessmentInterviewerMembers'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllIRFAssessmentInterviewerMembers
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO  
Create PROCEDURE [dbo].[usp_GetAllIRFAssessmentInterviewerMembers]    
@IRFId int    
AS    
Begin        
  select ti.IRFId, ul.EmailId, 
  ul.uniqueid,(ul.FirstName+' '+ul.LastName) as InterviewerName,ti.IsActive,ti.IsAvailable,  
  IsNull(DATEDIFF(SECOND, IRF.SendIntervieweravailbilityOn, GetDate()),0) as AvailbilityHour,IRF.JDLink,ti.PerSuccessRate  
  from TransInterviewer ti join InterviewerProfile ipr on ipr.UserId = ti.UserId        
  join UserList ul on ul.id = ti.UserId        
  join IRF on irf.id = ti.IRFId        
  where irf.id=@IRFId and ti.IsActive=1 and ul.IsActive=1       
End
GO