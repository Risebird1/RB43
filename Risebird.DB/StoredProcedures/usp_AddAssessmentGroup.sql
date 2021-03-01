IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddAssessmentGroup'))
BEGIN
    DROP PROCEDURE dbo.usp_AddAssessmentGroup
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



create Procedure usp_AddAssessmentGroup      
@AssessmentGroup varchar(100),      
@PrimarySkills varchar(max),
@id int
AS      
BEGIN  
if(@id=0)
Begin
Insert into AssessmentGroup values(@AssessmentGroup,@PrimarySkills)
END
else
Begin
Update AssessmentGroup set AssessmentGroup=@AssessmentGroup,PrimarySkills=@PrimarySkills where id=@id
end
END
GO