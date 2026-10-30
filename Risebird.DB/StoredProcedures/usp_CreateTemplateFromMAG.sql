IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_CreateTemplateFromMAG'))
BEGIN
    DROP PROCEDURE dbo.usp_CreateTemplateFromMAG
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_CreateTemplateFromMAG]    
@assessmentGroupId int,
@TechSkillBulkData AS [dbo].[TransTechSkill_TableTypeV5] READONLY,
@CreatedOn datetime,  
@CreatedBy varchar(100) = null--,
--@IsAadhaarVerificationRequired bit
AS    
BEGIN

if exists(select top 1 id from MasterAssessmentGroup where AssessmentGroupId = @AssessmentGroupId)
Begin
	if not exists(select top 1 csc.id from ClonableSkillChannel csc join IRF on irf.id= csc.CreatedFromIRFId join AssessmentGroup ag on ag.id = irf.AssessmentGroupId where ag.id = @AssessmentGroupId and (csc.IsDeleted is null or csc.IsDeleted =0))
	Begin
		declare @idClonableIrf int, @templateName varchar(100)
		set @idClonableIrf = 0
		set @templateName = (select AssessmentGroup from AssessmentGroup where id = @AssessmentGroupId) + ' Template'
  
		if not exists (select 1 from ClonableSkillChannel where [Name] = @templateName)       
		Begin     
  
		INSERT INTO [dbo].[ClonableSkillChannel]              
			([Name]              
			,[CreatedOn] 
			,[CreatedBy]   
			,[MeetingPlatform]
			,[IsAadhaarVerificationRequired]
			,[AssessmentGroupId])  
		VALUES              
		(@templateName,              
			@CreatedOn,   
			@CreatedBy,  
			'Zoho',
			1,
			@assessmentGroupId)        --@IsAadhaarVerificationRequired
         
		select @idClonableIrf = SCOPE_IDENTITY() 

		if(@idClonableIrf != 0)
		Begin


			delete from TransClonableIRFTechSkill where ClonableIRFId= @idClonableIrf  
  
				INSERT INTO [dbo].[TransClonableIRFTechSkill](      
				[ClonableIRFId] ,      
				[LevelId],      
				[SkillId],      
				[Weightage],[Concepts],[Natureofquestion],[Timelimit],[isEliminationskill], [SkillName])      
      
				SELECT @idClonableIrf, tsbd.LevelId, tsbd.[SkillId], tsbd.[Weightage], tsbd.[Concepts], tsbd.[Natureofquestion], tsbd.[Timelimit], tsbd.isEliminationskill, tsbd.SkillName  FROM @TechSkillBulkData tsbd


				delete from TransClonableIRFInterviewer where ClonableIRFId= @idClonableIrf  
  
			-- The MAG's own group plus its secondary groups, resolved once: the OR/IN-subquery
			-- filter over the split took ~42s on prod (same fix as usp_AddIRFDetails).
			declare @magAssessmentGroups table (AGId varchar(20) primary key)
			insert into @magAssessmentGroups (AGId)
			select cast(@AssessmentGroupId as varchar(20))
			union
			select cast(sag.AssessmentGroupId as varchar(20)) from SecondaryAssessmentGroups sag
			join MasterAssessmentGroup mag on mag.id = sag.MAGId
			where mag.AssessmentGroupId = @AssessmentGroupId and sag.AssessmentGroupId is not null

			;With CTE AS( Select id, AssessmentGroupId, UserId, value from InterviewerProfile cross Apply string_split(AssessmentGroupId,','))

			INSERT INTO [dbo].[TransClonableIRFInterviewer](
											[ClonableIRFId] ,
											[UserId],
											[PerSuccessRate],
											[IsActive],
											--[IsAvailable],
											[InterviewerId])
			Select Distinct @idClonableIrf, CTE.UserId, 0, 1, CTE.id
			From CTE join @magAssessmentGroups mags on CTE.value = mags.AGId
			join Userlist ul on CTE.UserId=ul.id
			where ul.IsActive=1
			group by CTE.UserId,CTE.AssessmentGroupId, CTE.id



			--questions 
			declare @createdByUserId int = (select id from UserList where EmailId = @CreatedBy)
					
			delete from ClonableTemplateQuestions where ClonableIRFId= @idClonableIrf  
  
				INSERT INTO [dbo].[ClonableTemplateQuestions](    
				[ClonableIRFId] ,    
				[UserId],    
				[Skill],    
				[Question], [SkillName])    
    
			Select distinct @idClonableIrf, @createdByUserId, 0, ts.Comment as Question, SkillName From TechSkillComments ts 
			join IRF on IRF.id=ts.IRFId --join Skill sk on sk.id=ts.Skill 
			where AssessmentGroupId = @assessmentGroupId 

		End
	End
End
END

End