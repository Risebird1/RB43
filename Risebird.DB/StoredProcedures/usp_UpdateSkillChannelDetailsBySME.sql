IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateSkillChannelDetailsBySME'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateSkillChannelDetailsBySME
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_UpdateSkillChannelDetailsBySME]    
(        
	@irfId int,
	@interviewerProfileId int,      
	@JDLink varchar(1000)=null,        
	@timeNow datetime,
	
	@TechSkillBulkData AS [dbo].[TransTechSkill_TableType] READONLY
	
)    
AS    
BEGIN    
  
  
 begin try  
  begin tran  

   declare @isJDLinkUpdated bit = 0, @areTechSkillsUpdated bit = 0

   declare @originalJDLink varchar(2000), @userId int
	select @originalJDLink = OriginalJDLink from IRF where id =@irfId
	set @userId = (select UserId from InterviewerProfile where id = @interviewerProfileId)

	if(@JDLink is not null )
	Begin
		if(@originalJDLink is null)
		begin
			update IRF set OriginalJDLink = JDLink, JDLink = @JDLink, UpdatedBySME = @userId, UpdatedBySMEAt = @timeNow  where id = @irfId
		end
		else
		Begin
			update IRF set JDLink = @JDLink, UpdatedBySME = @userId, UpdatedBySMEAt = @timeNow where id = @irfId

		End
		set @isJDLinkUpdated = 1
	End
		


if exists(
Select * from
(select IRFId, LevelId, SkillId, Weightage, Concepts, Natureofquestion, Timelimit from TransTechSkill where IRFId=@irfId
UNION ALL
select * from @TechSkillBulkData)
MergedTechTable
GROUP BY IRFId, LevelId, SkillId, Weightage, Concepts, Natureofquestion, Timelimit
HAVING COUNT(*) = 1)
Begin
	set @areTechSkillsUpdated = 1
End

	--tech skills
	delete from TransTechSkill where IRFId=@irfId

	INSERT INTO [dbo].[TransTechSkill](    
	[IRFId] ,    
	[LevelId],    
	[SkillId],    
	[Weightage],[Concepts],[Natureofquestion],[Timelimit])    
    
	SELECT * FROM @TechSkillBulkData 



	if not exists (select top 1 id from SMEActivityTracker where IRFId =@irfId and InterviewerProfileId = @interviewerProfileId)
	Begin
		insert into SMEActivityTracker (IRFId, InterviewerProfileId, IsJDLinkUploaded, AreTechSkillRequirementsUpdated)
		values (@irfId, @interviewerProfileId, @isJDLinkUpdated, @areTechSkillsUpdated)
	End
	Else
	Begin
		update SMEActivityTracker set IsJDLinkUploaded = @isJDLinkUpdated, AreTechSkillRequirementsUpdated = @areTechSkillsUpdated where InterviewerProfileId = @interviewerProfileId and IRFId = @irfId
	End

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


