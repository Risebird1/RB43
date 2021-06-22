  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SurveyClientDetails'))
BEGIN
    DROP PROCEDURE [dbo].[usp_SurveyClientDetails]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

    
Create PROCEDURE [dbo].[usp_SurveyClientDetails]    
(    
      @SurveyBulkDetails AS [dbo].[CuratedIRFMapping_TableType] READONLY    
)    
AS    
BEGIN    
    
SET NOCOUNT ON;    
  
Select im.Id as interviewid,cl.Name as ClientName,im.interviewdate from InterviewMeetings im join IRF on IRF.IRFID=im.IRFId join Client cl on cl.id=irf.ClientId join @SurveyBulkDetails sb on sb.CuratedGroupId=im.id      
    
END 