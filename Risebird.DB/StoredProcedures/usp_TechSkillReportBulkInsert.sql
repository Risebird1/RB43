/****** Object:  StoredProcedure [dbo].[usp_TechSkillReportBulkInsert]    Script Date: 25-06-2020 1.12.08 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_TechSkillReportBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_TechSkillReportBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_TechSkillReportBulkInsert]
(
      @TechSkillReportBulkData AS [dbo].[TechSkillReportV3_TableType] READONLY--TechSkillReportV2_TableType
)
AS
BEGIN

SET NOCOUNT ON;
 begin try  
  begin tran

	declare @interviewId int = (select top 1 InterviewId from @TechSkillReportBulkData)
	delete from TechSkillReport where InterviewId=@interviewId
	--This row performs bulk copy of object DataTable to Table in SQL
	INSERT INTO [dbo].[TechSkillReport](
		[InterviewId] ,
		[RequiredLevelId],
		[SkillId],
		[ActualLevelId],
		[WeightageRequired],
		[WeightageActual],
		[TechComments],
		[SkillSet],
		[SkillName])

	SELECT * FROM @TechSkillReportBulkData

 commit tran  
end try  
begin catch  
 
 rollback transaction  
 
 DECLARE @Message varchar(MAX) = ERROR_MESSAGE(),  
  @Severity int = ERROR_SEVERITY(),  
  @State smallint = ERROR_STATE()  
  
 RAISERROR (@Message, @Severity, @State)  
  
end catch  

END
GO


