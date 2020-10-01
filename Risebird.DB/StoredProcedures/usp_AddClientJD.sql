IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddClientJD'))
BEGIN
    DROP PROCEDURE [dbo].[usp_AddClientJD]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



CREATE PROCEDURE [dbo].[usp_AddClientJD]    
@StartDate Datetime,    
@EndDate Datetime,    
@AssessmentGroup int,    
@JobDescription varchar(MAX),  
@CreatedBy int,  
@ClientId int,  
@JDTitle varchar(200),  
@Filelink varchar(max)  
AS    
Begin    
 Insert Into Clientjd values(@StartDate,@EndDate,@JobDescription,@AssessmentGroup,@CreatedBy,Getdate(),@ClientId,@JDTitle,@Filelink)   
   
End 