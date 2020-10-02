IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddAssessmentGroup'))
BEGIN
    DROP PROCEDURE dbo.usp_AddAssessmentGroup
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create Procedure usp_AddAssessmentGroup    
@AssessmentGroup varchar(100),    
@PrimarySkills varchar(max)  
AS    
BEGIN    
Insert into AssessmentGroup values(@AssessmentGroup,@PrimarySkills)  
END
GO