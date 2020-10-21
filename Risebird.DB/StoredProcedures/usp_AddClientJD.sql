IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddClientJD'))
BEGIN
    DROP PROCEDURE [dbo].[usp_AddClientJD]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_AddClientJD]      
@id int,
@StartDate Datetime,      
@EndDate Datetime,      
@AssessmentGroup int,      
@JobDescription varchar(MAX),    
@CreatedBy int,    
@ClientId int,    
@JDTitle varchar(200),    
@Filelink varchar(max),
@Heuristicsscore int,
@JDBudget varchar(15),
@JobRole varchar(50),
@Location varchar(100),
@ClientUrl varchar(max)
AS      
Begin   
if(@id=0)
begin
 Insert Into Clientjd values(@StartDate,@EndDate,@JobDescription,@AssessmentGroup,@CreatedBy,Getdate(),@ClientId,@JDTitle,@Filelink ,@Heuristicsscore,@JDBudget,@JobRole,@Location,@ClientUrl)     
     end
	 else
	 Begin
	 Update Clientjd set StartDate=@StartDate,
	 EndDate=@EndDate,
	 JobDescription=@JobDescription,
	 AssessmentGroupid=@AssessmentGroup,
	 Craetedon=Getdate(),
	 JDTitle=@JDTitle,Filelink=@Filelink ,Heuristicsscore=@Heuristicsscore,JDBudget=@JDBudget,
	 JobRole=@JobRole,Location=@Location,ClientUrl=@ClientUrl
	Where id=@id
	 end
End   



GO